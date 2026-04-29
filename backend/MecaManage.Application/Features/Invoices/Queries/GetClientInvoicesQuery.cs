using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Invoices.Queries;

public record GetClientInvoicesQuery(
    Guid ClientId
) : IRequest<List<ClientInvoiceDto>>;

public record ClientInvoiceLineItemDto(
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal
);

public record ClientInvoiceDto(
    Guid Id,
    string InvoiceNumber,
    string GarageName,
    decimal ServiceFee,
    decimal PartsTotal,
    decimal TotalAmount,
    bool ClientApproved,
    string Status,
    DateTime CreatedAt,
    List<ClientInvoiceLineItemDto> LineItems
);

public class GetClientInvoicesQueryHandler : IRequestHandler<GetClientInvoicesQuery, List<ClientInvoiceDto>>
{
    private readonly IApplicationDbContext _context;

    public GetClientInvoicesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ClientInvoiceDto>> Handle(GetClientInvoicesQuery request, CancellationToken cancellationToken)
    {
        var invoices = await _context.Invoices
            .Where(i => i.ClientId == request.ClientId)
            .Include(i => i.Garage)
            .Include(i => i.LineItems)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

        return invoices.Select(i => new ClientInvoiceDto(
            i.Id,
            i.InvoiceNumber,
            i.Garage.Name,
            i.ServiceFee,
            i.PartsTotal,
            i.TotalAmount,
            i.ClientApproved,
            i.Status.ToString(),
            i.CreatedAt,
            i.LineItems.Select(li => new ClientInvoiceLineItemDto(
                li.Description,
                li.Quantity,
                li.UnitPrice,
                li.LineTotal
            )).ToList()
        )).ToList();
    }
}

