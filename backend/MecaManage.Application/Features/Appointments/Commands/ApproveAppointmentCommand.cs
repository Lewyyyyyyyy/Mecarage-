using MecaManage.Application.Common.Interfaces;
using MecaManage.Application.Features.InterventionLifecycle.Commands;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MecaManage.Application.Features.Appointments.Commands;

public record ApproveAppointmentCommand(
    Guid AppointmentId,
    Guid ChefId
) : IRequest<ApproveAppointmentResult>;

public record ApproveAppointmentResult(bool Success, string Message);

public class ApproveAppointmentCommandHandler : IRequestHandler<ApproveAppointmentCommand, ApproveAppointmentResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IMediator _mediator;
    private readonly ILogger<ApproveAppointmentCommandHandler> _logger;

    public ApproveAppointmentCommandHandler(
        IApplicationDbContext context,
        IMediator mediator,
        ILogger<ApproveAppointmentCommandHandler> logger)
    {
        _context  = context;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ApproveAppointmentResult> Handle(ApproveAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Garage)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);

        if (appointment == null)
            return new ApproveAppointmentResult(false, "Rendez-vous introuvable");

        // Idempotent behavior: repeated approve calls should not fail.
        if (appointment.Status == AppointmentStatus.Approved)
            return new ApproveAppointmentResult(true, "Rendez-vous déjà approuvé");

        // Verify chef belongs to the garage
        var chefBelongsToGarage = await _context.Users
            .AnyAsync(u => u.Id == request.ChefId && u.GarageId == appointment.GarageId && u.Role == UserRole.ChefAtelier, cancellationToken);

        if (!chefBelongsToGarage)
            return new ApproveAppointmentResult(false, "Vous n'avez pas les permissions pour approuver ce rendez-vous");

        appointment.Status           = AppointmentStatus.Approved;
        appointment.ApprovedByChefId = request.ChefId;
        appointment.ApprovedAt       = DateTime.UtcNow;

        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync(cancellationToken);

        // Keep approval successful even if the optional lifecycle tracker cannot be created.
        try
        {
            if (appointment.Garage != null)
            {
                await _mediator.Send(new CreateInterventionLifecycleCommand(
                    TenantId:        appointment.Garage.TenantId,
                    GarageId:        appointment.GarageId,
                    ClientId:        appointment.ClientId,
                    VehicleId:       appointment.VehicleId,
                    AppointmentId:   appointment.Id,
                    SymptomReportId: appointment.SymptomReportId
                ), cancellationToken);
            }
            else
            {
                _logger.LogWarning(
                    "Appointment {AppointmentId} approved but Garage navigation is null; lifecycle tracker skipped.",
                    appointment.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Appointment {AppointmentId} approved, but lifecycle tracker creation failed.",
                appointment.Id);
        }

        return new ApproveAppointmentResult(true, "Rendez-vous approuvé avec succès");
    }
}
