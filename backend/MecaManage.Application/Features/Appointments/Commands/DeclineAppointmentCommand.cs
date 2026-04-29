using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Appointments.Commands;

public record DeclineAppointmentCommand(
    Guid AppointmentId,
    Guid ChefId,
    string DeclineReason
) : IRequest<DeclineAppointmentResult>;

public record DeclineAppointmentResult(bool Success, string Message);

public class DeclineAppointmentCommandHandler : IRequestHandler<DeclineAppointmentCommand, DeclineAppointmentResult>
{
    private readonly IApplicationDbContext _context;

    public DeclineAppointmentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DeclineAppointmentResult> Handle(DeclineAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);

        if (appointment == null)
            return new DeclineAppointmentResult(false, "Rendez-vous introuvable");

        // Verify chef belongs to the garage
        var chefBelongsToGarage = await _context.Users
            .AnyAsync(u => u.Id == request.ChefId && u.GarageId == appointment.GarageId && u.Role == UserRole.ChefAtelier, cancellationToken);

        if (!chefBelongsToGarage)
            return new DeclineAppointmentResult(false, "Vous n'avez pas les permissions pour refuser ce rendez-vous");

        appointment.Status = AppointmentStatus.Declined;
        appointment.DeclineReason = request.DeclineReason;

        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync(cancellationToken);

        return new DeclineAppointmentResult(true, "Rendez-vous refusé");
    }
}

