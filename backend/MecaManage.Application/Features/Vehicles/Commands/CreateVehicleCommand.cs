using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Vehicles.Commands;

public record CreateVehicleCommand(
    Guid ClientId,
    string Brand,
    string Model,
    int Year,
    string LicensePlate,
    string FuelType,
    int Mileage,
    string? VIN
) : IRequest<CreateVehicleResult>;

public record CreateVehicleResult(bool Success, string Message, Guid? VehicleId);

public class CreateVehicleCommandHandler : IRequestHandler<CreateVehicleCommand, CreateVehicleResult>
{
    private readonly IApplicationDbContext _context;

    public CreateVehicleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CreateVehicleResult> Handle(CreateVehicleCommand request, CancellationToken cancellationToken)
    {
        // Validate clientId
        if (request.ClientId == Guid.Empty)
            return new CreateVehicleResult(false, "ID utilisateur invalide", null);

        var clientExists = await _context.Users
            .AnyAsync(u => u.Id == request.ClientId && !u.IsDeleted, cancellationToken);

        if (!clientExists)
            return new CreateVehicleResult(false, "Utilisateur non trouvé", null);

        var plateExists = await _context.Vehicles
            .AnyAsync(v => v.LicensePlate == request.LicensePlate, cancellationToken);

        if (plateExists)
            return new CreateVehicleResult(false, "Plaque d'immatriculation déjà enregistrée", null);

        var vehicle = new Vehicle
        {
            ClientId = request.ClientId,
            Brand = request.Brand,
            Model = request.Model,
            Year = request.Year,
            LicensePlate = request.LicensePlate,
            FuelType = request.FuelType,
            Mileage = request.Mileage,
            VIN = request.VIN
        };

        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateVehicleResult(true, "Véhicule ajouté avec succès", vehicle.Id);
    }
}