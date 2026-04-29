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
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

        if (invoice == null)
            return new ApproveInvoiceResult(false, "Facture introuvable");

        if (invoice.ClientId != request.ClientId)
            return new ApproveInvoiceResult(false, "Vous n'avez pas accès à cette facture");

        if (invoice.Status != InvoiceStatus.AwaitingApproval)
            return new ApproveInvoiceResult(false, "Seules les factures en attente d'approbation peuvent être approuvées");

        invoice.ClientApproved = true;
        invoice.ClientApprovedAt = DateTime.UtcNow;
        invoice.Status = InvoiceStatus.ClientApproved;

        _context.Invoices.Update(invoice);

        // Find the repair task linked to this appointment to notify the chef and mechanics
        var repairTask = await _context.RepairTasks
            .Include(t => t.Assignments)
            .FirstOrDefaultAsync(t => t.AppointmentId == invoice.AppointmentId, cancellationToken);

        if (repairTask != null)
        {
            // Notify the chef
            var chefNotification = new Notification
            {
                RecipientId = repairTask.AssignedByChefId,
                InvoiceId = invoice.Id,
                RepairTaskId = repairTask.Id,
                Title = "✅ Devis approuvé — Réparation autorisée",
                Message = $"Le client a approuvé le devis (Facture {invoice.InvoiceNumber}). Vous pouvez maintenant assigner des mécaniciens pour effectuer les réparations.",
                NotificationType = "InvoiceApprovedByClient",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            _context.Notifications.Add(chefNotification);

            // Notify each assigned mechanic that repair can now begin
            foreach (var assignment in repairTask.Assignments)
            {
                var mechanicNotification = new Notification
                {
                    RecipientId = assignment.MechanicId,
                    RepairTaskId = repairTask.Id,
                    Title = "🔧 Réparation autorisée — À commencer",
                    Message = $"Le client a approuvé le devis pour la tâche « {repairTask.TaskTitle} ». Vous pouvez maintenant commencer les réparations.",
                    NotificationType = "RepairAuthorized",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                _context.Notifications.Add(mechanicNotification);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new ApproveInvoiceResult(true, "Facture approuvée avec succès");
    }
}
