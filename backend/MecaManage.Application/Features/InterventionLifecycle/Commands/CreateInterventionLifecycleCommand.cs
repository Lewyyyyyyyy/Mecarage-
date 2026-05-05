using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Entities;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.InterventionLifecycle.Commands;

/// <summary>
/// Creates an Intervention tracker record when a garage appointment is approved.
/// Called automatically from ApproveAppointmentCommandHandler.
/// </summary>
public record CreateInterventionLifecycleCommand(
    Guid TenantId,
    Guid GarageId,
    Guid ClientId,
    Guid VehicleId,
    Guid AppointmentId,
    Guid? SymptomReportId
) : IRequest<Guid?>;

public class CreateInterventionLifecycleCommandHandler
    : IRequestHandler<CreateInterventionLifecycleCommand, Guid?>
{
    private readonly IApplicationDbContext _context;
    public CreateInterventionLifecycleCommandHandler(IApplicationDbContext context)
        => _context = context;

    public async Task<Guid?> Handle(
        CreateInterventionLifecycleCommand request,
        CancellationToken cancellationToken)
    {
        // Guard: don't create duplicate trackers for the same appointment
        var exists = await _context.Interventions
            .AnyAsync(i => i.AppointmentId == request.AppointmentId, cancellationToken);

        if (exists) return null;

        var intervention = new Intervention
        {
            TenantId        = request.TenantId,
            GarageId        = request.GarageId,
            ClientId        = request.ClientId,
            VehicleId       = request.VehicleId,
            AppointmentId   = request.AppointmentId,
            SymptomReportId = request.SymptomReportId,
            Status          = InterventionLifecycleStatus.Created
        };

        _context.Interventions.Add(intervention);
        await _context.SaveChangesAsync(cancellationToken);
        return intervention.Id;
    }
}

