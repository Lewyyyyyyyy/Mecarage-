using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Garages.Queries;

public record GetAllGaragesQuery : IRequest<List<GarageDto>>;

public class GetAllGaragesQueryHandler : IRequestHandler<GetAllGaragesQuery, List<GarageDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAllGaragesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<GarageDto>> Handle(GetAllGaragesQuery request, CancellationToken cancellationToken)
    {
        var garages = await _context.Garages
            .AsNoTracking()
            .Include(g => g.Admin)
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);

        return garages.Select(g => new GarageDto(
            g.Id,
            g.TenantId,
            g.Name,
            g.Address,
            g.City,
            g.Phone,
            g.Latitude,
            g.Longitude,
            g.IsActive,
            g.AdminId,
            g.Admin != null ? g.Admin.FirstName : null,
            g.Admin != null ? g.Admin.LastName : null
        )).ToList();
    }
}

