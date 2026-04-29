using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Invoices.Queries;

public record GetGarageInvoicesQuery(
    Guid GarageId,
    Guid ChefId
) : IRequest<List<GarageInvoiceDto>>;

public record GarageInvoiceDto(
    Guid Id,
    string InvoiceNumber,
    string ClientName,
    decimal ServiceFee,
    decimal PartsTotal,
    decimal TotalAmount,
    bool ClientApproved,
    string Status,
    DateTime CreatedAt
);

public class GetGarageInvoicesQueryHandler : IRequestHandler<GetGarageInvoicesQuery, List<GarageInvoiceDto>>
{
    private readonly IApplicationDbContext _context;

    public GetGarageInvoicesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<GarageInvoiceDto>> Handle(GetGarageInvoicesQuery request, CancellationToken cancellationToken)
    {
        // Verify chef belongs to garage
        var chefBelongsToGarage = await _context.Users
            .AnyAsync(u => u.Id == request.ChefId && u.GarageId == request.GarageId, cancellationToken);

        if (!chefBelongsToGarage)
            return new List<GarageInvoiceDto>();

        return await _context.Invoices
            .Where(i => i.GarageId == request.GarageId)
            .Include(i => i.Client)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new GarageInvoiceDto(
                i.Id,
                i.InvoiceNumber,
                $"{i.Client.FirstName} {i.Client.LastName}",
                i.ServiceFee,
                i.PartsTotal,
                i.TotalAmount,
                i.ClientApproved,
                i.Status.ToString(),
                i.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }
}

