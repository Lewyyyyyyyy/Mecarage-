using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Garages.Queries;

public record GetTenantGaragesQuery(Guid TenantId) : IRequest<List<GarageDto>>;


public class GetTenantGaragesQueryHandler : IRequestHandler<GetTenantGaragesQuery, List<GarageDto>>
{
    private readonly IApplicationDbContext _context;

    public GetTenantGaragesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<GarageDto>> Handle(GetTenantGaragesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Garages
            .Where(g => g.TenantId == request.TenantId)
            .Include(g => g.Admin)
            .Select(g => new GarageDto(
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
            ))
            .ToListAsync(cancellationToken);
    }
}

