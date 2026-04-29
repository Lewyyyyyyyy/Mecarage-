using MecaManage.Application.Common.Interfaces;
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

    public ApproveAppointmentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApproveAppointmentResult> Handle(ApproveAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);

        if (appointment == null)
            return new ApproveAppointmentResult(false, "Rendez-vous introuvable");

        // Verify chef belongs to the garage
        var chefBelongsToGarage = await _context.Users
            .AnyAsync(u => u.Id == request.ChefId && u.GarageId == appointment.GarageId && u.Role == UserRole.ChefAtelier, cancellationToken);

        if (!chefBelongsToGarage)
            return new ApproveAppointmentResult(false, "Vous n'avez pas les permissions pour approuver ce rendez-vous");

        appointment.Status = AppointmentStatus.Approved;
        appointment.ApprovedByChefId = request.ChefId;
        appointment.ApprovedAt = DateTime.UtcNow;

        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync(cancellationToken);

        return new ApproveAppointmentResult(true, "Rendez-vous approuvé avec succès");
    }
}

