using System.Text.Json;
using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Entities;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.RepairTasks.Commands;

public record TaskPartDto(
    Guid SparePartId,
    string Name,
    int Quantity,
    decimal UnitPrice
);

public record UpdateMechanicTaskCommand(
    Guid TaskId,
    Guid MechanicId,
    bool SubmitToChef = false,
    string? MechanicNotes = null,
    string? FileUrl = null,
    List<TaskPartDto>? Parts = null
) : IRequest<UpdateMechanicTaskResult>;

public record UpdateMechanicTaskResult(bool Success, string Message);

public class UpdateMechanicTaskCommandHandler : IRequestHandler<UpdateMechanicTaskCommand, UpdateMechanicTaskResult>
{
    private const decimal ServiceFee = 50m;   // Fixed labour / examination fee

    private readonly IApplicationDbContext _context;

    public UpdateMechanicTaskCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UpdateMechanicTaskResult> Handle(UpdateMechanicTaskCommand request, CancellationToken cancellationToken)
    {
        // ── 1. Load & validate task ───────────────────────────────────────────
        var task = await _context.RepairTasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task == null)
            return new UpdateMechanicTaskResult(false, "Tâche introuvable");

        var assignment = await _context.RepairTaskAssignments
            .FirstOrDefaultAsync(a => a.RepairTaskId == request.TaskId && a.MechanicId == request.MechanicId, cancellationToken);

        if (assignment == null)
            return new UpdateMechanicTaskResult(false, "Vous n'êtes pas assigné à cette tâche");

        // If task is still Assigned, auto-start it
        if (task.Status == RepairTaskStatus.Assigned)
        {
            task.Status    = RepairTaskStatus.InProgress;
            task.StartedAt = DateTime.UtcNow;
        }

        if (assignment.StartedWorkAt == null)
            assignment.StartedWorkAt = DateTime.UtcNow;

        // ── 2. Update assignment fields ──────────────────────────────────────
        if (request.MechanicNotes != null)
        {
            assignment.MechanicNotes           = request.MechanicNotes;
            assignment.ExaminationObservations = request.MechanicNotes;
        }

        if (request.FileUrl != null)
            assignment.ExaminationFileUrl = request.FileUrl;

        if (request.Parts != null)
            assignment.PartsNeeded = JsonSerializer.Serialize(request.Parts);

        // ── 3. Submit to chef when explicitly requested ──────────────────────
        if (request.SubmitToChef && assignment.ExaminationStatus is "None" or null or "DeclinedByChef")
        {
            assignment.ExaminationStatus      = "Pending";
            assignment.ExaminationSubmittedAt = DateTime.UtcNow;
        }


        // ── 4. Parts → draft invoice (upsert, regardless of submit/save) ────
        if (request.Parts != null && request.Parts.Count > 0)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == task.AppointmentId, cancellationToken);

            if (appointment == null)
                return new UpdateMechanicTaskResult(false, "Rendez-vous introuvable");

            var partsTotal = request.Parts.Sum(p => p.UnitPrice * p.Quantity);
            var total      = ServiceFee + partsTotal;

            // Upsert invoice — only allowed while still in Draft
            var existingInvoice = await _context.Invoices
                .Include(i => i.LineItems)
                .FirstOrDefaultAsync(i => i.AppointmentId == task.AppointmentId, cancellationToken);

            if (existingInvoice != null && existingInvoice.Status != InvoiceStatus.Draft)
                return new UpdateMechanicTaskResult(false,
                    "Une facture a déjà été envoyée au client pour ce rendez-vous et ne peut plus être modifiée");

            Guid invoiceId;

            if (existingInvoice != null)
            {
                // Remove existing line items first
                foreach (var li in existingInvoice.LineItems.ToList())
                    _context.InvoiceLineItems.Remove(li);

                existingInvoice.ServiceFee  = ServiceFee;
                existingInvoice.PartsTotal  = partsTotal;
                existingInvoice.TotalAmount = total;
                existingInvoice.UpdatedAt   = DateTime.UtcNow;
                foreach (var part in request.Parts)
                {
                    var li = BuildLineItem(part);
                    li.InvoiceId = existingInvoice.Id;
                    _context.InvoiceLineItems.Add(li);
                }

                invoiceId = existingInvoice.Id;
            }
            else
            {
                var invoice = new Invoice
                {
                    AppointmentId = task.AppointmentId,
                    TenantId      = task.TenantId,
                    ClientId      = appointment.ClientId,
                    GarageId      = task.GarageId,
                    InvoiceNumber = $"INV-{DateTime.UtcNow:yyyy-MM}-{Guid.NewGuid().ToString()[..5].ToUpper()}",
                    ServiceFee    = ServiceFee,
                    PartsTotal    = partsTotal,
                    TotalAmount   = total,
                    Status        = InvoiceStatus.Draft,
                    CreatedAt     = DateTime.UtcNow,
                };
                foreach (var part in request.Parts)
                    invoice.LineItems.Add(BuildLineItem(part));
                _context.Invoices.Add(invoice);
                invoiceId = invoice.Id;
            }

            // Notify chef only when mechanic explicitly submits
            if (request.SubmitToChef)
            {
                _context.Notifications.Add(new Notification
                {
                    RecipientId      = task.AssignedByChefId,
                    RepairTaskId     = task.Id,
                    InvoiceId        = invoiceId,
                    Title            = "🔍 Rapport mécanicien — À valider avant envoi client",
                    Message          = $"Le mécanicien a soumis son rapport pour « {task.TaskTitle} ». " +
                                       $"Devis préparé : {total:F2} € ({ServiceFee:F2} € main d'œuvre + {partsTotal:F2} € pièces). " +
                                       $"Modifiez si nécessaire, puis validez pour envoyer le devis au client.",
                    NotificationType = "InvoiceReadyForChefValidation",
                    CreatedAt        = DateTime.UtcNow,
                    IsRead           = false
                });
            }
        }
        else if (request.SubmitToChef)
        {
            // Submitted without parts — still notify chef so they can review
            _context.Notifications.Add(new Notification
            {
                RecipientId      = task.AssignedByChefId,
                RepairTaskId     = task.Id,
                Title            = "🔍 Rapport mécanicien — À valider",
                Message          = $"Le mécanicien a soumis son rapport pour « {task.TaskTitle} » sans pièces à facturer. Veuillez valider.",
                NotificationType = "InvoiceReadyForChefValidation",
                CreatedAt        = DateTime.UtcNow,
                IsRead           = false
            });
        }

        // ── 5. Single atomic save ────────────────────────────────────────────
        await _context.SaveChangesAsync(cancellationToken);

        var msg = request.SubmitToChef
            ? "Rapport soumis au chef pour validation"
            : "Progression sauvegardée";
        return new UpdateMechanicTaskResult(true, msg);
    }

    private static InvoiceLineItem BuildLineItem(TaskPartDto part) => new()
    {
        SparePartId = part.SparePartId,
        Description = part.Name,
        Quantity    = part.Quantity,
        UnitPrice   = part.UnitPrice,
        LineTotal   = part.Quantity * part.UnitPrice,
    };
}
