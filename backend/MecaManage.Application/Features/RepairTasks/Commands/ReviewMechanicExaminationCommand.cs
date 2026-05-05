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
    string? DeclineReason = null,
    string? UpdatedObservations = null,
    List<ReviewPartInputDto>? UpdatedParts = null
) : IRequest<ReviewExaminationResult>;

public record ReviewExaminationResult(bool Success, string Message, Guid? InvoiceId = null);

public record ReviewPartInputDto(Guid? SparePartId, string Name, int Quantity, decimal UnitPrice);

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

        // ── Apply chef's edits to the assignment ──────────────────────────────
        if (request.UpdatedObservations != null)
        {
            pendingAssignment.ExaminationObservations = request.UpdatedObservations;
            pendingAssignment.MechanicNotes           = request.UpdatedObservations;
        }

        if (request.UpdatedParts != null && request.UpdatedParts.Count > 0)
        {
            var opts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            pendingAssignment.PartsNeeded = JsonSerializer.Serialize(request.UpdatedParts, opts);
        }

        // Get the appointment client
        var appointment = repairTask.Appointment;

        if (request.IsApproved)
        {
            pendingAssignment.ExaminationStatus = "ApprovedByChef";

            // ── Use existing Draft invoice if already created by mechanic flow ──
            var existingDraft = await _context.Invoices
                .Include(i => i.LineItems)
                .FirstOrDefaultAsync(i => i.AppointmentId == repairTask.AppointmentId
                                       && i.Status == InvoiceStatus.Draft, cancellationToken);

            Guid invoiceId;
            decimal totalAmount;

            if (existingDraft != null)
            {
                // If chef provided updated parts, rebuild line items
                if (request.UpdatedParts != null && request.UpdatedParts.Count > 0)
                {
                    foreach (var li in existingDraft.LineItems.ToList())
                        _context.InvoiceLineItems.Remove(li);

                    decimal partsTotal = 0m;
                    foreach (var p in request.UpdatedParts)
                    {
                        _context.InvoiceLineItems.Add(new InvoiceLineItem
                        {
                            InvoiceId   = existingDraft.Id,
                            SparePartId = p.SparePartId,   // nullable Guid — OK
                            Description = p.Name,
                            Quantity    = p.Quantity,
                            UnitPrice   = p.UnitPrice,
                            LineTotal   = p.Quantity * p.UnitPrice
                        });
                        partsTotal += p.Quantity * p.UnitPrice;
                    }
                    existingDraft.PartsTotal  = partsTotal;
                    existingDraft.ServiceFee  = request.ServiceFee;
                    existingDraft.TotalAmount = request.ServiceFee + partsTotal;
                }
                else
                {
                    // Just update service fee if it changed
                    existingDraft.ServiceFee  = request.ServiceFee;
                    existingDraft.TotalAmount = request.ServiceFee + existingDraft.PartsTotal;
                }

                existingDraft.Status      = InvoiceStatus.AwaitingApproval;
                existingDraft.FinalizedAt = DateTime.UtcNow;
                invoiceId   = existingDraft.Id;
                totalAmount = existingDraft.TotalAmount;
            }
            else
            {
                // No draft from mechanic — build from assignment data
                var lineItems   = new List<InvoiceLineItemData>();
                decimal partsTotal = 0;

                var partsSource = request.UpdatedParts;
                if (partsSource != null && partsSource.Count > 0)
                {
                    foreach (var p in partsSource)
                    {
                        lineItems.Add(new InvoiceLineItemData(p.Name, p.Quantity, p.UnitPrice));
                        partsTotal += p.Quantity * p.UnitPrice;
                    }
                }
                else if (!string.IsNullOrEmpty(pendingAssignment.PartsNeeded))
                {
                    try
                    {
                        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var rawParts = JsonSerializer.Deserialize<List<JsonElement>>(pendingAssignment.PartsNeeded, opts);
                        if (rawParts != null)
                        {
                            foreach (var p in rawParts)
                            {
                                string name  = GetStr(p, "name") ?? "";
                                int    qty   = GetInt(p, "quantity");
                                decimal price = GetDec(p, "unitPrice") ?? GetDec(p, "estimatedPrice") ?? 0m;
                                lineItems.Add(new InvoiceLineItemData(name, qty, price));
                                partsTotal += qty * price;
                            }
                        }
                    }
                    catch { }
                }

                var invoice = new Invoice
                {
                    AppointmentId = repairTask.AppointmentId,
                    TenantId      = repairTask.TenantId,
                    ClientId      = appointment.ClientId,
                    GarageId      = repairTask.GarageId,
                    InvoiceNumber = $"INV-{DateTime.UtcNow:yyyy-MM}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                    ServiceFee    = request.ServiceFee,
                    PartsTotal    = partsTotal,
                    TotalAmount   = request.ServiceFee + partsTotal,
                    Status        = InvoiceStatus.AwaitingApproval,
                    FinalizedAt   = DateTime.UtcNow
                };
                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync(cancellationToken);

                foreach (var item in lineItems)
                    _context.InvoiceLineItems.Add(new InvoiceLineItem
                    {
                        InvoiceId   = invoice.Id,
                        Description = item.Description,
                        Quantity    = item.Quantity,
                        UnitPrice   = item.UnitPrice,
                        LineTotal   = item.Quantity * item.UnitPrice
                    });

                invoiceId   = invoice.Id;
                totalAmount = invoice.TotalAmount;
            }

            // Notify client
            _context.Notifications.Add(new Notification
            {
                RecipientId      = appointment.ClientId,
                InvoiceId        = invoiceId,
                RepairTaskId     = repairTask.Id,
                Title            = "📋 Devis disponible — Votre accord est requis",
                Message          = $"Le chef d'atelier a validé le rapport du mécanicien et préparé un devis de {totalAmount:F2} €. " +
                                   $"Veuillez l'approuver ou le refuser.",
                NotificationType = "InvoiceReady",
                CreatedAt        = DateTime.UtcNow,
                IsRead           = false
            });

            // ── Advance intervention to ExaminationReviewed + set examination details ──
            var intervention = await _context.Interventions
                .FirstOrDefaultAsync(i => i.AppointmentId == repairTask.AppointmentId, cancellationToken);
            if (intervention != null)
            {
                intervention.ExaminationNotes = request.UpdatedObservations ?? pendingAssignment.ExaminationObservations;
                intervention.PartsUsedJson    = pendingAssignment.PartsNeeded;
                intervention.Status           = InterventionLifecycleStatus.ExaminationReviewed;
                _context.Interventions.Update(intervention);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return new ReviewExaminationResult(true, "Rapport approuvé — devis envoyé au client", invoiceId);
        }
        else
        {
            pendingAssignment.ExaminationStatus = "DeclinedByChef";

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

    private static string? GetStr(JsonElement el, string key)
    {
        foreach (var p in el.EnumerateObject())
            if (string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase)) return p.Value.GetString();
        return null;
    }
    private static int GetInt(JsonElement el, string key)
    {
        foreach (var p in el.EnumerateObject())
            if (string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase) && p.Value.TryGetInt32(out var v)) return v;
        return 0;
    }
    private static decimal? GetDec(JsonElement el, string key)
    {
        foreach (var p in el.EnumerateObject())
            if (string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase) && p.Value.TryGetDecimal(out var v)) return v;
        return null;
    }
}

