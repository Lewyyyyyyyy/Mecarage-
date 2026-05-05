using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Invoices.Queries;

public record GetInvoicePdfQuery(
    Guid InvoiceId,
    Guid RequestingUserId
) : IRequest<InvoicePdfDto?>;

public record InvoicePdfLineItemDto(
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal
);

public record InvoicePdfDto(
    Guid Id,
    string InvoiceNumber,
    DateTime CreatedAt,
    DateTime? FinalizedAt,
    string Status,
    // Garage info
    string GarageName,
    string GarageAddress,
    string GarageCity,
    string GaragePhone,
    // Client info
    string ClientName,
    string ClientEmail,
    string ClientPhone,
    // Amounts
    decimal ServiceFee,
    decimal PartsTotal,
    decimal? TaxAmount,
    decimal TotalAmount,
    // Line items
    List<InvoicePdfLineItemDto> LineItems
);

public class GetInvoicePdfQueryHandler : IRequestHandler<GetInvoicePdfQuery, InvoicePdfDto?>
{
    private readonly IApplicationDbContext _context;

    public GetInvoicePdfQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InvoicePdfDto?> Handle(GetInvoicePdfQuery request, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Garage)
            .Include(i => i.Client)
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

        if (invoice is null) return null;

        // Allow access: client who owns it, or garage staff
        var isOwner = invoice.ClientId == request.RequestingUserId;
        var isStaff = await _context.Users
            .AnyAsync(u => u.Id == request.RequestingUserId && u.GarageId == invoice.GarageId, cancellationToken);

        if (!isOwner && !isStaff) return null;

        return new InvoicePdfDto(
            Id: invoice.Id,
            InvoiceNumber: invoice.InvoiceNumber,
            CreatedAt: invoice.CreatedAt,
            FinalizedAt: invoice.FinalizedAt,
            Status: invoice.Status.ToString(),
            GarageName: invoice.Garage.Name,
            GarageAddress: invoice.Garage.Address,
            GarageCity: invoice.Garage.City,
            GaragePhone: invoice.Garage.Phone,
            ClientName: $"{invoice.Client.FirstName} {invoice.Client.LastName}",
            ClientEmail: invoice.Client.Email,
            ClientPhone: invoice.Client.Phone,
            ServiceFee: invoice.ServiceFee,
            PartsTotal: invoice.PartsTotal,
            TaxAmount: invoice.TaxAmount,
            TotalAmount: invoice.TotalAmount,
            LineItems: invoice.LineItems.Select(li => new InvoicePdfLineItemDto(
                li.Description,
                li.Quantity,
                li.UnitPrice,
                li.LineTotal
            )).ToList()
        );
    }
}

