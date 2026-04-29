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

        invoice.Status = InvoiceStatus.ClientRejected;
        _context.Invoices.Update(invoice);

        // Notify chef of rejection
        var repairTask = await _context.RepairTasks
            .FirstOrDefaultAsync(t => t.AppointmentId == invoice.AppointmentId, cancellationToken);

        if (repairTask != null)
        {
            var chefNotification = new Notification
            {
                RecipientId = repairTask.AssignedByChefId,
                InvoiceId = invoice.Id,
                Title = "Devis refusé par le client",
                Message = $"Le client a refusé le devis #{invoice.InvoiceNumber}. La réparation ne sera pas effectuée.",
                NotificationType = "InvoiceRejectedByClient",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            _context.Notifications.Add(chefNotification);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return new RejectInvoiceResult(true, "Devis refusé avec succès");
    }
}

