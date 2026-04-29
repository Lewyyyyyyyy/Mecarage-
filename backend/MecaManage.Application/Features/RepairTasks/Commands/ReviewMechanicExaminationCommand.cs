using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Entities;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MecaManage.Application.Features.RepairTasks.Commands;

public record ReviewMechanicExaminationCommand(
    Guid RepairTaskId,
    Guid ChefId,
    bool IsApproved,
    decimal ServiceFee,
    string? DeclineReason = null
) : IRequest<ReviewExaminationResult>;

public record ReviewExaminationResult(bool Success, string Message, Guid? InvoiceId = null);

public class ReviewMechanicExaminationCommandHandler : IRequestHandler<ReviewMechanicExaminationCommand, ReviewExaminationResult>
{
    private readonly IApplicationDbContext _context;

    public ReviewMechanicExaminationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReviewExaminationResult> Handle(ReviewMechanicExaminationCommand request, CancellationToken cancellationToken)
    {
        var repairTask = await _context.RepairTasks
            .Include(t => t.Appointment)
            .Include(t => t.Assignments)
            .FirstOrDefaultAsync(t => t.Id == request.RepairTaskId && t.AssignedByChefId == request.ChefId, cancellationToken);

        if (repairTask == null)
            return new ReviewExaminationResult(false, "Tâche non trouvée ou accès refusé");

        var pendingAssignment = repairTask.Assignments
            .FirstOrDefault(a => a.ExaminationStatus == "Pending");

        if (pendingAssignment == null)
            return new ReviewExaminationResult(false, "Aucun rapport d'examen en attente");

        // Get the appointment client
        var appointment = repairTask.Appointment;

        if (request.IsApproved)
        {
            pendingAssignment.ExaminationStatus = "ApprovedByChef";
            _context.RepairTaskAssignments.Update(pendingAssignment);

            // Parse parts needed to create invoice line items
            var lineItems = new List<InvoiceLineItemData>();
            decimal partsTotal = 0;

            if (!string.IsNullOrEmpty(pendingAssignment.PartsNeeded))
            {
                try
                {
                    var parts = JsonSerializer.Deserialize<List<ExaminationPartDto>>(pendingAssignment.PartsNeeded);
                    if (parts != null)
                    {
                        foreach (var part in parts)
                        {
                            lineItems.Add(new InvoiceLineItemData(part.Name, part.Quantity, part.EstimatedPrice));
                            partsTotal += part.Quantity * part.EstimatedPrice;
                        }
                    }
                }
                catch { /* If parsing fails, continue with empty parts */ }
            }

            // Create invoice
            var invoice = new Invoice
            {
                AppointmentId = repairTask.AppointmentId,
                TenantId = repairTask.TenantId,
                ClientId = appointment.ClientId,
                GarageId = repairTask.GarageId,
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                ServiceFee = request.ServiceFee,
                PartsTotal = partsTotal,
                TotalAmount = request.ServiceFee + partsTotal,
                Status = InvoiceStatus.AwaitingApproval,
                FinalizedAt = DateTime.UtcNow
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync(cancellationToken);

            // Add line items
            foreach (var item in lineItems)
            {
                var lineItem = new InvoiceLineItem
                {
                    InvoiceId = invoice.Id,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    LineTotal = item.Quantity * item.UnitPrice
                };
                _context.InvoiceLineItems.Add(lineItem);
            }

            // Notify client
            var clientNotification = new Notification
            {
                RecipientId = appointment.ClientId,
                InvoiceId = invoice.Id,
                RepairTaskId = repairTask.Id,
                Title = "Devis approuvé - Votre accord est requis",
                Message = $"Le chef d'atelier a préparé un devis pour la réparation. Montant total: {invoice.TotalAmount:F2} EUR. Veuillez approuver ou refuser.",
                NotificationType = "InvoiceReady",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(clientNotification);
            await _context.SaveChangesAsync(cancellationToken);

            return new ReviewExaminationResult(true, "Examen approuvé et devis envoyé au client", invoice.Id);
        }
        else
        {
            pendingAssignment.ExaminationStatus = "DeclinedByChef";
            _context.RepairTaskAssignments.Update(pendingAssignment);

            // Create $50 examination fee invoice
            var examinationInvoice = new Invoice
            {
                AppointmentId = repairTask.AppointmentId,
                TenantId = repairTask.TenantId,
                ClientId = appointment.ClientId,
                GarageId = repairTask.GarageId,
                InvoiceNumber = $"INV-EXAM-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                ServiceFee = 50m,
                PartsTotal = 0,
                TotalAmount = 50m,
                Status = InvoiceStatus.AwaitingApproval,
                FinalizedAt = DateTime.UtcNow
            };

            _context.Invoices.Add(examinationInvoice);
            await _context.SaveChangesAsync(cancellationToken);

            var examLineItem = new InvoiceLineItem
            {
                InvoiceId = examinationInvoice.Id,
                Description = "Frais d'examen",
                Quantity = 1,
                UnitPrice = 50m,
                LineTotal = 50m
            };
            _context.InvoiceLineItems.Add(examLineItem);

            // Update appointment status
            var apt = await _context.Appointments.FindAsync([repairTask.AppointmentId], cancellationToken);
            if (apt != null)
            {
                apt.Status = AppointmentStatus.Declined;
                _context.Appointments.Update(apt);
            }

            // Notify client
            var clientNotification = new Notification
            {
                RecipientId = appointment.ClientId,
                InvoiceId = examinationInvoice.Id,
                RepairTaskId = repairTask.Id,
                Title = "Réparation refusée - Frais d'examen",
                Message = $"Après examen, le chef d'atelier a décliné la réparation. {(request.DeclineReason != null ? $"Raison: {request.DeclineReason}. " : "")}Des frais d'examen de 50 EUR vous seront facturés.",
                NotificationType = "ExaminationDeclined",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(clientNotification);
            await _context.SaveChangesAsync(cancellationToken);

            return new ReviewExaminationResult(true, "Examen refusé et facture d'examen créée", examinationInvoice.Id);
        }
    }

    private record InvoiceLineItemData(string Description, int Quantity, decimal UnitPrice);
}

