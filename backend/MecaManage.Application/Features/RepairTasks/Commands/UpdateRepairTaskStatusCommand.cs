using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Entities;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.RepairTasks.Commands;

public record UpdateRepairTaskStatusCommand(
    Guid TaskId,
    Guid MechanicId,
    RepairTaskStatus NewStatus,
    string? CompletionNotes = null
) : IRequest<UpdateRepairTaskStatusResult>;

public record UpdateRepairTaskStatusResult(bool Success, string Message);

public class UpdateRepairTaskStatusCommandHandler : IRequestHandler<UpdateRepairTaskStatusCommand, UpdateRepairTaskStatusResult>
{
    private readonly IApplicationDbContext _context;

    public UpdateRepairTaskStatusCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UpdateRepairTaskStatusResult> Handle(UpdateRepairTaskStatusCommand request, CancellationToken cancellationToken)
    {
        var task = await _context.RepairTasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task == null)
            return new UpdateRepairTaskStatusResult(false, "Tâche introuvable");

        // Verify mechanic is assigned to this task
        var isAssigned = await _context.RepairTaskAssignments
            .AnyAsync(a => a.RepairTaskId == request.TaskId && a.MechanicId == request.MechanicId, cancellationToken);

        if (!isAssigned)
            return new UpdateRepairTaskStatusResult(false, "Vous n'êtes pas assigné à cette tâche");

        // Validate state machine transitions
        var validTransitions = GetValidTransitions(task.Status);
        if (!validTransitions.Contains(request.NewStatus))
            return new UpdateRepairTaskStatusResult(false, $"Transition de {task.Status} vers {request.NewStatus} n'est pas autorisée");

        // Update task status
        task.Status = request.NewStatus;
        task.CompletionNotes = request.CompletionNotes ?? task.CompletionNotes;

        // Update timestamps
        if (request.NewStatus == RepairTaskStatus.InProgress && task.StartedAt == null)
            task.StartedAt = DateTime.UtcNow;

        if (request.NewStatus == RepairTaskStatus.Done)
            task.CompletedAt = DateTime.UtcNow;

        // Update mechanic assignment timestamps
        var assignment = await _context.RepairTaskAssignments
            .FirstOrDefaultAsync(a => a.RepairTaskId == request.TaskId && a.MechanicId == request.MechanicId, cancellationToken);

        if (assignment != null)
        {
            if (request.NewStatus == RepairTaskStatus.InProgress && assignment.StartedWorkAt == null)
                assignment.StartedWorkAt = DateTime.UtcNow;

            if (request.NewStatus == RepairTaskStatus.Done)
                assignment.CompletedWorkAt = DateTime.UtcNow;

            _context.RepairTaskAssignments.Update(assignment);
        }

        _context.RepairTasks.Update(task);

        // Notify chef when mechanic marks work as Fixed (ready for test)
        if (request.NewStatus == RepairTaskStatus.Fixed)
        {
            var chefNotification = new Notification
            {
                RecipientId = task.AssignedByChefId,
                RepairTaskId = task.Id,
                Title = "Réparation terminée — à tester",
                Message = $"Le mécanicien a terminé la réparation de la tâche « {task.TaskTitle} ». Veuillez effectuer le test qualité avant livraison.",
                NotificationType = "WorkReadyForTest",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            _context.Notifications.Add(chefNotification);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new UpdateRepairTaskStatusResult(true, $"Tâche mise à jour vers {request.NewStatus}");
    }

    private List<RepairTaskStatus> GetValidTransitions(RepairTaskStatus currentStatus)
    {
        return currentStatus switch
        {
            RepairTaskStatus.Assigned => new() { RepairTaskStatus.InProgress, RepairTaskStatus.OnHold, RepairTaskStatus.Cancelled },
            RepairTaskStatus.InProgress => new() { RepairTaskStatus.Fixed, RepairTaskStatus.OnHold, RepairTaskStatus.Cancelled },
            RepairTaskStatus.Fixed => new() { RepairTaskStatus.Tested, RepairTaskStatus.OnHold },
            RepairTaskStatus.Tested => new() { RepairTaskStatus.Done, RepairTaskStatus.InProgress },
            RepairTaskStatus.OnHold => new() { RepairTaskStatus.InProgress, RepairTaskStatus.Cancelled },
            RepairTaskStatus.Done => new() { RepairTaskStatus.Done }, // No further transitions
            RepairTaskStatus.Cancelled => new() { RepairTaskStatus.Cancelled }, // No further transitions
            _ => new()
        };
    }
}

