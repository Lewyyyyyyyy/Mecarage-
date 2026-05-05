using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Entities;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Invoices.Commands;

public record ApproveInvoiceCommand(
    Guid InvoiceId,
    Guid ClientId
) : IRequest<ApproveInvoiceResult>;

public record ApproveInvoiceResult(bool Success, string Message);

public class ApproveInvoiceCommandHandler : IRequestHandler<ApproveInvoiceCommand, ApproveInvoiceResult>
{
    private readonly IApplicationDbContext _context;

    public ApproveInvoiceCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApproveInvoiceResult> Handle(ApproveInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

        if (invoice == null)
            return new ApproveInvoiceResult(false, "Facture introuvable");

        if (invoice.ClientId != request.ClientId)
            return new ApproveInvoiceResult(false, "Vous n'avez pas accès à cette facture");

        if (invoice.Status != InvoiceStatus.AwaitingApproval)
            return new ApproveInvoiceResult(false, "Seules les factures en attente d'approbation peuvent être approuvées");

        invoice.ClientApproved   = true;
        invoice.ClientApprovedAt = DateTime.UtcNow;
        invoice.Status           = InvoiceStatus.ClientApproved;
        _context.Invoices.Update(invoice);

        // ── Deduct stock for every part line item now that client approved ────
        foreach (var lineItem in invoice.LineItems.Where(li => li.SparePartId.HasValue))
        {
            var sparePart = await _context.SpareParts
                .FirstOrDefaultAsync(sp => sp.Id == lineItem.SparePartId!.Value, cancellationToken);

            if (sparePart != null)
            {
                sparePart.StockQuantity = Math.Max(0, sparePart.StockQuantity - lineItem.Quantity);
                _context.SpareParts.Update(sparePart);
            }
        }

        // ── Notify chef + mechanics to start work ────────────────────────────
        var repairTask = await _context.RepairTasks
            .Include(t => t.Assignments)
            .FirstOrDefaultAsync(t => t.AppointmentId == invoice.AppointmentId, cancellationToken);

        if (repairTask != null)
        {
            _context.Notifications.Add(new Notification
            {
                RecipientId      = repairTask.AssignedByChefId,
                InvoiceId        = invoice.Id,
                RepairTaskId     = repairTask.Id,
                Title            = "✅ Devis approuvé — Réparation autorisée",
                Message          = $"Le client a approuvé le devis (Facture {invoice.InvoiceNumber}, {invoice.TotalAmount:F2} €). " +
                                   $"Le stock a été mis à jour. Vous pouvez lancer les réparations.",
                NotificationType = "InvoiceApprovedByClient",
                CreatedAt        = DateTime.UtcNow,
                IsRead           = false
            });

            foreach (var assignment in repairTask.Assignments)
            {
                _context.Notifications.Add(new Notification
                {
                    RecipientId      = assignment.MechanicId,
                    RepairTaskId     = repairTask.Id,
                    Title            = "🔧 Devis approuvé — Commencez les réparations",
                    Message          = $"Le client a approuvé le devis pour la tâche « {repairTask.TaskTitle} ». " +
                                       $"Les pièces sont réservées. Vous pouvez commencer les réparations.",
                    NotificationType = "RepairAuthorized",
                    CreatedAt        = DateTime.UtcNow,
                    IsRead           = false
                });
            }
        }

        // ── Advance intervention tracker: client approved → repair will start ─
        var intervention = await _context.Interventions
            .FirstOrDefaultAsync(i => i.AppointmentId == invoice.AppointmentId, cancellationToken);
        if (intervention != null)
        {
            intervention.ProceedWithIntervention = true;
            intervention.Status                  = InterventionLifecycleStatus.RepairInProgress;
            _context.Interventions.Update(intervention);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new ApproveInvoiceResult(true, "Facture approuvée avec succès");
    }
}
