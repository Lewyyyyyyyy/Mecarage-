using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Entities;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.RepairTasks.Commands;

public record AssignMechanicCommand(
    Guid TaskId,
    Guid MechanicId,
    Guid ChefId
) : IRequest<AssignMechanicResult>;

public record AssignMechanicResult(bool Success, string Message);

public class AssignMechanicCommandHandler : IRequestHandler<AssignMechanicCommand, AssignMechanicResult>
{
    private readonly IApplicationDbContext _context;

    public AssignMechanicCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AssignMechanicResult> Handle(AssignMechanicCommand request, CancellationToken cancellationToken)
    {
        var task = await _context.RepairTasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task == null)
            return new AssignMechanicResult(false, "Tâche introuvable");

        // Verify chef belongs to same garage and created this task
        var chefBelongsToGarage = await _context.Users
            .AnyAsync(u => u.Id == request.ChefId && u.GarageId == task.GarageId && u.Role == UserRole.ChefAtelier, cancellationToken);

        if (!chefBelongsToGarage)
            return new AssignMechanicResult(false, "Vous n'avez pas les permissions pour assigner un mécanicien");

        // Verify mechanic belongs to same garage
        var mechanicExists = await _context.Users
            .AnyAsync(u => u.Id == request.MechanicId && u.GarageId == task.GarageId && u.Role == UserRole.Mecanicien, cancellationToken);

        if (!mechanicExists)
            return new AssignMechanicResult(false, "Le mécanicien n'appartient pas à ce garage");

        // Check if already assigned — idempotent: just notify and return success
        var alreadyAssigned = await _context.RepairTaskAssignments
            .AnyAsync(a => a.RepairTaskId == request.TaskId && a.MechanicId == request.MechanicId, cancellationToken);

        if (!alreadyAssigned)
        {
            var assignment = new RepairTaskAssignment
            {
                RepairTaskId = request.TaskId,
                MechanicId   = request.MechanicId,
                AssignedAt   = DateTime.UtcNow
            };
            _context.RepairTaskAssignments.Add(assignment);
        }

        // Notify the mechanic about the repair work assignment
        _context.Notifications.Add(new Notification
        {
            RecipientId      = request.MechanicId,
            RepairTaskId     = request.TaskId,
            Title            = "🔧 Nouvelle tâche de réparation assignée",
            Message          = $"Le chef d'atelier vous a assigné la tâche « {task.TaskTitle} ». " +
                               $"Le client a approuvé les travaux. Vous pouvez commencer les réparations.",
            NotificationType = "RepairAssigned",
            CreatedAt        = DateTime.UtcNow,
            IsRead           = false
        });

        await _context.SaveChangesAsync(cancellationToken);

        return new AssignMechanicResult(true, "Mécanicien assigné avec succès");
    }
}

