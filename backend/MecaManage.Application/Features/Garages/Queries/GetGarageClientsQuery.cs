using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Garages.Queries;

public record GetGarageClientsQuery(Guid GarageId) : IRequest<List<GarageClientDto>>;

public record GarageClientDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    int VehicleCount,
    int InterventionCount
);

public class GetGarageClientsQueryHandler : IRequestHandler<GetGarageClientsQuery, List<GarageClientDto>>
{
    private readonly IApplicationDbContext _context;

    public GetGarageClientsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<GarageClientDto>> Handle(GetGarageClientsQuery request, CancellationToken cancellationToken)
    {
        // Get unique client IDs from interventions in this garage
        var clientIds = await _context.InterventionRequests
            .Where(ir => ir.GarageId == request.GarageId)
            .Select(ir => ir.ClientId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (!clientIds.Any())
            return new List<GarageClientDto>();

        // Load clients (ignore soft-delete filter so we still see them)
        var users = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => clientIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        // Load vehicle counts per client
        var vehicleCounts = await _context.Vehicles
            .Where(v => clientIds.Contains(v.ClientId))
            .GroupBy(v => v.ClientId)
            .Select(g => new { ClientId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        // Load intervention counts per client for this garage
        var interventionCounts = await _context.InterventionRequests
            .Where(ir => ir.GarageId == request.GarageId && clientIds.Contains(ir.ClientId))
            .GroupBy(ir => ir.ClientId)
            .Select(g => new { ClientId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var vcMap = vehicleCounts.ToDictionary(x => x.ClientId, x => x.Count);
        var icMap = interventionCounts.ToDictionary(x => x.ClientId, x => x.Count);

        return users
            .Select(u => new GarageClientDto(
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.Phone,
                vcMap.TryGetValue(u.Id, out var vc) ? vc : 0,
                icMap.TryGetValue(u.Id, out var ic) ? ic : 0
            ))
            .OrderByDescending(c => c.InterventionCount)
            .ToList();
    }
}

