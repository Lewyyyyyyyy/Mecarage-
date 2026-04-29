using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Vehicles.Queries;

public record GetVehiclesQuery(Guid ClientId) : IRequest<List<VehicleDto>>;

public record VehicleDto(Guid Id, Guid ClientId, string Brand, string Model, int Year, string LicensePlate, string FuelType, int Mileage, string? VIN);

public class GetVehiclesQueryHandler : IRequestHandler<GetVehiclesQuery, List<VehicleDto>>
{
    private readonly IApplicationDbContext _context;

    public GetVehiclesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<VehicleDto>> Handle(GetVehiclesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Vehicles
            .Where(v => v.ClientId == request.ClientId)
            .Select(v => new VehicleDto(v.Id, v.ClientId, v.Brand, v.Model, v.Year, v.LicensePlate, v.FuelType, v.Mileage, v.VIN))
            .ToListAsync(cancellationToken);
    }
}