using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Garages.Queries;

public record GetGarageInterventionsQuery(Guid GarageId) : IRequest<List<GarageInterventionDto>>;

public record GarageInterventionDto(
    Guid Id,
    string ClientName,
    string ClientEmail,
    string VehicleInfo,
    string Status,
    string? Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public class GetGarageInterventionsQueryHandler : IRequestHandler<GetGarageInterventionsQuery, List<GarageInterventionDto>>
{
    private readonly IApplicationDbContext _context;

    public GetGarageInterventionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<GarageInterventionDto>> Handle(GetGarageInterventionsQuery request, CancellationToken cancellationToken)
    {
        // Use explicit join to avoid null navigation issues caused by soft-delete query filters
        var interventions = await _context.InterventionRequests
            .Where(ir => ir.GarageId == request.GarageId)
            .OrderByDescending(ir => ir.CreatedAt)
            .ToListAsync(cancellationToken);

        var clientIds = interventions.Select(ir => ir.ClientId).Distinct().ToList();
        var vehicleIds = interventions.Select(ir => ir.VehicleId).Distinct().ToList();

        var clients = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => clientIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        var vehicles = await _context.Vehicles
            .IgnoreQueryFilters()
            .Where(v => vehicleIds.Contains(v.Id))
            .ToListAsync(cancellationToken);

        var clientMap = clients.ToDictionary(c => c.Id);
        var vehicleMap = vehicles.ToDictionary(v => v.Id);

        return interventions.Select(ir =>
        {
            clientMap.TryGetValue(ir.ClientId, out var client);
            vehicleMap.TryGetValue(ir.VehicleId, out var vehicle);
            return new GarageInterventionDto(
                ir.Id,
                client != null ? client.FirstName + " " + client.LastName : "Client inconnu",
                client?.Email ?? "",
                vehicle != null ? vehicle.Brand + " " + vehicle.Model + " (" + vehicle.Year + ")" : "Véhicule inconnu",
                ir.Status.ToString(),
                ir.Description,
                ir.CreatedAt,
                ir.UpdatedAt
            );
        }).ToList();
    }
}



