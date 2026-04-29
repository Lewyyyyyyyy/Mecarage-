using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.SpareParts.Queries;

public record GetGarageSparePartsQuery(Guid GarageId, string? Category = null, string? Search = null)
    : IRequest<List<SparePartDto>>;

public record SparePartDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string Category,
    decimal UnitPrice,
    int StockQuantity,
    int ReorderLevel,
    string? Manufacturer,
    string? PartNumber,
    bool IsActive,
    bool IsLowStock,
    DateTime LastRestocked
);

public class GetGarageSparePartsQueryHandler : IRequestHandler<GetGarageSparePartsQuery, List<SparePartDto>>
{
    private readonly IApplicationDbContext _context;

    public GetGarageSparePartsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SparePartDto>> Handle(GetGarageSparePartsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.SpareParts
            .Where(sp => sp.GarageId == request.GarageId);

        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(sp => sp.Category == request.Category);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(sp =>
                sp.Name.ToLower().Contains(s) ||
                sp.Code.ToLower().Contains(s) ||
                (sp.PartNumber != null && sp.PartNumber.ToLower().Contains(s)));
        }

        var parts = await query
            .OrderBy(sp => sp.Category)
            .ThenBy(sp => sp.Name)
            .ToListAsync(cancellationToken);

        return parts.Select(sp => new SparePartDto(
            sp.Id,
            sp.Code,
            sp.Name,
            sp.Description,
            sp.Category,
            sp.UnitPrice,
            sp.StockQuantity,
            sp.ReorderLevel,
            sp.Manufacturer,
            sp.PartNumber,
            sp.IsActive,
            sp.StockQuantity <= sp.ReorderLevel,
            sp.LastRestocked
        )).ToList();
    }
}

