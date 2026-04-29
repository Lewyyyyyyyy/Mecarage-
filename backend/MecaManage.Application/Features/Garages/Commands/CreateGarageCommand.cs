using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Garages.Commands;

/// <summary>
/// Command to create a new garage (repair workshop).
/// </summary>
/// <param name="Name">The name of the garage (e.g., "Central Workshop"). Must be at least 3 characters.</param>
/// <param name="Address">Street address of the garage.</param>
/// <param name="City">City where the garage is located.</param>
/// <param name="Phone">Contact phone number for the garage.</param>
/// <param name="Latitude">Geographic latitude coordinate (optional).</param>
/// <param name="Longitude">Geographic longitude coordinate (optional).</param>
/// <param name="TenantId">The ID of the tenant (company) this garage belongs to.</param>
public record CreateGarageCommand(
    string Name,
    string Address,
    string City,
    string Phone,
    double? Latitude,
    double? Longitude,
    Guid TenantId
) : IRequest<CreateGarageResult>;

/// <summary>
/// Result of creating a garage.
/// </summary>
/// <param name="Success">Indicates if the garage was created successfully.</param>
/// <param name="Message">Descriptive message about the result.</param>
/// <param name="GarageId">The ID of the newly created garage (null if creation failed).</param>
public record CreateGarageResult(bool Success, string Message, Guid? GarageId);

public class CreateGarageCommandHandler : IRequestHandler<CreateGarageCommand, CreateGarageResult>
{
    private readonly IApplicationDbContext _context;

    public CreateGarageCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CreateGarageResult> Handle(CreateGarageCommand request, CancellationToken cancellationToken)
    {
        // Validate tenantId is not empty
        if (request.TenantId == Guid.Empty)
            return new CreateGarageResult(false, "TenantId is required", null);

        var tenantExists = await _context.Tenants
            .AnyAsync(t => t.Id == request.TenantId, cancellationToken);

        if (!tenantExists)
            return new CreateGarageResult(false, "Tenant introuvable", null);

        var garage = new Garage
        {
            TenantId = request.TenantId,
            Name = request.Name,
            Address = request.Address,
            City = request.City,
            Phone = request.Phone,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsActive = true
        };

        _context.Garages.Add(garage);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateGarageResult(true, "Garage créé avec succès", garage.Id);
    }
}

