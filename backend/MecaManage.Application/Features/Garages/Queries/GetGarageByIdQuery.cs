using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Garages.Queries;

public record GetGarageByIdQuery(Guid GarageId) : IRequest<List<GarageDto>>;

public class GetGarageByIdQueryHandler : IRequestHandler<GetGarageByIdQuery, List<GarageDto>>
{
    private readonly IApplicationDbContext _context;

    public GetGarageByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<GarageDto>> Handle(GetGarageByIdQuery request, CancellationToken cancellationToken)
    {
        var garage = await _context.Garages
            .Where(g => g.Id == request.GarageId)
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
            .FirstOrDefaultAsync(cancellationToken);

        return garage != null ? new List<GarageDto> { garage } : new List<GarageDto>();
    }
}

