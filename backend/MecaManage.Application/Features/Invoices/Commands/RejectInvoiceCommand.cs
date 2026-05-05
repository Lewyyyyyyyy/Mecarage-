using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Entities;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Invoices.Commands;

public record RejectInvoiceCommand(Guid InvoiceId, Guid ClientId) : IRequest<RejectInvoiceResult>;

public record RejectInvoiceResult(bool Success, string Message);

public class RejectInvoiceCommandHandler : IRequestHandler<RejectInvoiceCommand, RejectInvoiceResult>
{
    private readonly IApplicationDbContext _context;

    public RejectInvoiceCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RejectInvoiceResult> Handle(RejectInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Appointment)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId && i.ClientId == request.ClientId, cancellationToken);

        if (invoice == null)
            return new RejectInvoiceResult(false, "Facture introuvable ou accès refusé");

        if (invoice.Status != InvoiceStatus.AwaitingApproval)
            return new RejectInvoiceResult(false, "Cette facture ne peut plus être modifiée");

        // Client declined the repair — no parts will be used, no payment due
        invoice.Status      = InvoiceStatus.ClientRejected;
        invoice.ServiceFee  = 0m;  // examination was free
        invoice.PartsTotal  = 0m;
        invoice.TotalAmount = 0m;
        _context.Invoices.Update(invoice);

        var repairTask = await _context.RepairTasks
            .Include(t => t.Assignments)
            .FirstOrDefaultAsync(t => t.AppointmentId == invoice.AppointmentId, cancellationToken);

        if (repairTask != null)
        {
            // Notify chef
            _context.Notifications.Add(new Notification
            {
                RecipientId      = repairTask.AssignedByChefId,
                InvoiceId        = invoice.Id,
                RepairTaskId     = repairTask.Id,
                Title            = "❌ Devis refusé par le client",
                Message          = $"Le client a refusé le devis #{invoice.InvoiceNumber}. " +
                                   $"Aucune pièce ne sera utilisée — le stock reste inchangé. " +
                                   $"Montant facturé : 0 €.",
                NotificationType = "InvoiceRejectedByClient",
                CreatedAt        = DateTime.UtcNow,
                IsRead           = false
            });

            // Notify each assigned mechanic
            foreach (var assignment in repairTask.Assignments)
            {
                _context.Notifications.Add(new Notification
                {
                    RecipientId      = assignment.MechanicId,
                    RepairTaskId     = repairTask.Id,
                    Title            = "❌ Devis refusé — Réparation annulée",
                    Message          = $"Le client a refusé le devis pour la tâche « {repairTask.TaskTitle} ». " +
                                       $"Aucune intervention ne sera effectuée. Le stock n'a pas été modifié.",
                    NotificationType = "InvoiceRejectedByClient",
                    CreatedAt        = DateTime.UtcNow,
                    IsRead           = false
                });
            }
        }

        // ── Advance intervention tracker: client rejected ─────────────────
        var intervention = await _context.Interventions
            .FirstOrDefaultAsync(i => i.AppointmentId == invoice.AppointmentId, cancellationToken);
        if (intervention != null)
        {
            intervention.ProceedWithIntervention = false;
            intervention.Status                  = InterventionLifecycleStatus.Rejected;
            _context.Interventions.Update(intervention);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return new RejectInvoiceResult(true, "Devis refusé — aucun paiement requis");
    }
}

