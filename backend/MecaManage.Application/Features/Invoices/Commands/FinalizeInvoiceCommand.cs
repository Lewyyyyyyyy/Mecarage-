using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Invoices.Commands;

public record FinalizeInvoiceCommand(
    Guid InvoiceId,
    Guid ChefId
) : IRequest<FinalizeInvoiceResult>;

public record FinalizeInvoiceResult(bool Success, string Message);

public class FinalizeInvoiceCommandHandler : IRequestHandler<FinalizeInvoiceCommand, FinalizeInvoiceResult>
{
    private readonly IApplicationDbContext _context;

    public FinalizeInvoiceCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FinalizeInvoiceResult> Handle(FinalizeInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Garage)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

        if (invoice == null)
            return new FinalizeInvoiceResult(false, "Facture introuvable");

        // Verify chef belongs to the garage
        var chefBelongsToGarage = await _context.Users
            .AnyAsync(u => u.Id == request.ChefId && u.GarageId == invoice.GarageId && u.Role == UserRole.ChefAtelier, cancellationToken);

        if (!chefBelongsToGarage)
            return new FinalizeInvoiceResult(false, "Vous n'avez pas les permissions pour finaliser cette facture");

        if (invoice.Status != InvoiceStatus.Draft)
            return new FinalizeInvoiceResult(false, "Seules les factures en brouillon peuvent être finalisées");

        invoice.Status = InvoiceStatus.AwaitingApproval;
        invoice.FinalizedAt = DateTime.UtcNow;

        _context.Invoices.Update(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        return new FinalizeInvoiceResult(true, "Facture finalisée et envoyée au client");
    }
}

