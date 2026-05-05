using MecaManage.Application.Common.Interfaces;
using MecaManage.Application.Features.InterventionLifecycle.Commands;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

    public ApproveAppointmentCommandHandler(IApplicationDbContext context, IMediator mediator)
    {
        _context  = context;
        _mediator = mediator;
    }

    public async Task<ApproveAppointmentResult> Handle(ApproveAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Garage)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);

        if (appointment == null)
            return new ApproveAppointmentResult(false, "Rendez-vous introuvable");

        // Verify chef belongs to the garage
        var chefBelongsToGarage = await _context.Users
            .AnyAsync(u => u.Id == request.ChefId && u.GarageId == appointment.GarageId && u.Role == UserRole.ChefAtelier, cancellationToken);

        if (!chefBelongsToGarage)
            return new ApproveAppointmentResult(false, "Vous n'avez pas les permissions pour approuver ce rendez-vous");

        appointment.Status          = AppointmentStatus.Approved;
        appointment.ApprovedByChefId = request.ChefId;
        appointment.ApprovedAt       = DateTime.UtcNow;

        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync(cancellationToken);

        // ── Auto-create the intervention lifecycle tracker ────────────────
        await _mediator.Send(new CreateInterventionLifecycleCommand(
            TenantId:        appointment.Garage.TenantId,
            GarageId:        appointment.GarageId,
            ClientId:        appointment.ClientId,
            VehicleId:       appointment.VehicleId,
            AppointmentId:   appointment.Id,
            SymptomReportId: appointment.SymptomReportId
        ), cancellationToken);

        return new ApproveAppointmentResult(true, "Rendez-vous approuvé avec succès");
    }
}

