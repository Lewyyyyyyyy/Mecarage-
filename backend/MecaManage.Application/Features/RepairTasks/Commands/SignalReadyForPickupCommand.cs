using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Entities;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.RepairTasks.Commands;

public record SignalReadyForPickupCommand(
    Guid RepairTaskId,
    Guid ChefId
) : IRequest<SignalReadyResult>;

public record SignalReadyResult(bool Success, string Message);

public class SignalReadyForPickupCommandHandler : IRequestHandler<SignalReadyForPickupCommand, SignalReadyResult>
{
    private readonly IApplicationDbContext _context;

    public SignalReadyForPickupCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SignalReadyResult> Handle(SignalReadyForPickupCommand request, CancellationToken cancellationToken)
    {
        var repairTask = await _context.RepairTasks
            .Include(t => t.Appointment)
            .FirstOrDefaultAsync(t => t.Id == request.RepairTaskId && t.AssignedByChefId == request.ChefId, cancellationToken);

        if (repairTask == null)
            return new SignalReadyResult(false, "Tâche non trouvée ou accès refusé");

        if (repairTask.Status != RepairTaskStatus.Fixed && repairTask.Status != RepairTaskStatus.Tested)
            return new SignalReadyResult(false, "La tâche doit être réparée (Fixed) ou testée avant de signaler la disponibilité");

        // Update task and appointment status — mark as Tested + Done in one step
        repairTask.Status = RepairTaskStatus.Done;
        repairTask.CompletedAt = DateTime.UtcNow;
        _context.RepairTasks.Update(repairTask);

        var appointment = repairTask.Appointment;
        appointment.Status = AppointmentStatus.Completed;
        _context.Appointments.Update(appointment);

        // Notify client to come pick up their car
        var clientNotification = new Notification
        {
            RecipientId = appointment.ClientId,
            AppointmentId = appointment.Id,
            RepairTaskId = repairTask.Id,
            Title = "🚗 Votre véhicule est prêt à être récupéré !",
            Message = "Les réparations sont terminées et validées par le chef d'atelier. Vous pouvez venir récupérer votre voiture et procéder au paiement.",
            NotificationType = "ReadyForPickup",
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.Notifications.Add(clientNotification);

        // ── Advance intervention tracker to ReadyForPickup ────────────────
        var intervention = await _context.Interventions
            .FirstOrDefaultAsync(i => i.AppointmentId == appointment.Id, cancellationToken);
        if (intervention != null)
        {
            intervention.Status = InterventionLifecycleStatus.ReadyForPickup;
            _context.Interventions.Update(intervention);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new SignalReadyResult(true, "Client notifié que le véhicule est prêt");
    }
}

