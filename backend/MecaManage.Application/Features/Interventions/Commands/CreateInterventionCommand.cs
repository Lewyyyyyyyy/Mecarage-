using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Entities;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Interventions.Commands;

public record CreateInterventionCommand(
    Guid ClientId,
    Guid VehicleId,
    Guid GarageId,
    string Description,
    UrgencyLevel UrgencyLevel,
    DateTime? AppointmentDate
) : IRequest<CreateInterventionResult>;

public record CreateInterventionResult(bool Success, string Message, Guid? InterventionId);

public class CreateInterventionCommandHandler : IRequestHandler<CreateInterventionCommand, CreateInterventionResult>
{
    private readonly IApplicationDbContext _context;

    public CreateInterventionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CreateInterventionResult> Handle(CreateInterventionCommand request, CancellationToken cancellationToken)
    {
        var vehicleExists = await _context.Vehicles
            .AnyAsync(v => v.Id == request.VehicleId, cancellationToken);

        if (!vehicleExists)
            return new CreateInterventionResult(false, "Véhicule introuvable", null);

        var garageExists = await _context.Garages
            .AnyAsync(g => g.Id == request.GarageId, cancellationToken);

        if (!garageExists)
            return new CreateInterventionResult(false, "Garage introuvable", null);

        var intervention = new InterventionRequest
        {
            ClientId = request.ClientId,
            VehicleId = request.VehicleId,
            GarageId = request.GarageId,
            Description = request.Description,
            UrgencyLevel = request.UrgencyLevel,
            AppointmentDate = request.AppointmentDate,
            Status = InterventionStatus.EnAttente
        };

        _context.InterventionRequests.Add(intervention);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateInterventionResult(true, "Demande d'intervention créée", intervention.Id);
    }
}