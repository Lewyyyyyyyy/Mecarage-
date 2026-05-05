using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.InterventionLifecycle.Commands;

/// <summary>
/// Registers payment when the client picks up their car.
/// Transitions the intervention to Closed.
/// Callable by Admin/ChefAtelier roles only.
/// </summary>
public record RegisterPaymentCommand(
    Guid InterventionId,
    decimal PaymentAmount,
    string PaymentMethod,
    string PaidBy
) : IRequest<RegisterPaymentResult>;

public record RegisterPaymentResult(bool Success, string Message);

public class RegisterPaymentCommandHandler
    : IRequestHandler<RegisterPaymentCommand, RegisterPaymentResult>
{
    private readonly IApplicationDbContext _context;
    public RegisterPaymentCommandHandler(IApplicationDbContext context)
        => _context = context;

    public async Task<RegisterPaymentResult> Handle(
        RegisterPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var intervention = await _context.Interventions
            .FirstOrDefaultAsync(i => i.Id == request.InterventionId, cancellationToken);

        if (intervention == null)
            return new RegisterPaymentResult(false, "Intervention introuvable");

        if (intervention.Status == InterventionLifecycleStatus.Closed)
            return new RegisterPaymentResult(false, "Cette intervention est déjà clôturée");

        // Also update the linked invoice to Paid if present
        if (intervention.InvoiceId.HasValue)
        {
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == intervention.InvoiceId.Value, cancellationToken);
            if (invoice != null && invoice.Status != InvoiceStatus.Paid)
            {
                invoice.Status = InvoiceStatus.Paid;
                _context.Invoices.Update(invoice);
            }
        }

        intervention.PaymentAmount = request.PaymentAmount;
        intervention.PaymentMethod = request.PaymentMethod;
        intervention.PaymentDate   = DateTime.UtcNow;
        intervention.PaidBy        = request.PaidBy;
        intervention.Status        = InterventionLifecycleStatus.Closed;

        _context.Interventions.Update(intervention);
        await _context.SaveChangesAsync(cancellationToken);

        return new RegisterPaymentResult(true, "Paiement enregistré. Intervention clôturée.");
    }
}

