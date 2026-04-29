using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.RepairTasks.Queries;

public record GetRepairTaskDetailsQuery(
    Guid TaskId,
    Guid UserId
) : IRequest<RepairTaskDetailsDto?>;

public record RepairTaskDetailsDto(
    Guid Id,
    string TaskTitle,
    string Description,
    string Status,
    string ClientName,
    string VehicleInfo,
    DateTime AssignedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? CompletionNotes,
    List<AssignedMechanicDto> Mechanics,
    int? EstimatedMinutes,
    int? ActualMinutes
);

public record AssignedMechanicDto(
    Guid Id,
    string FullName,
    DateTime? StartedWork,
    DateTime? CompletedWork
);

public class GetRepairTaskDetailsQueryHandler : IRequestHandler<GetRepairTaskDetailsQuery, RepairTaskDetailsDto?>
{
    private readonly IApplicationDbContext _context;

    public GetRepairTaskDetailsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RepairTaskDetailsDto?> Handle(GetRepairTaskDetailsQuery request, CancellationToken cancellationToken)
    {
        var task = await _context.RepairTasks
            .Where(t => t.Id == request.TaskId)
            .Include(t => t.Appointment)
            .ThenInclude(a => a.Client)
            .Include(t => t.Appointment)
            .ThenInclude(a => a.Vehicle)
            .Include(t => t.Assignments)
            .ThenInclude(a => a.Mechanic)
            .FirstOrDefaultAsync(cancellationToken);

        if (task == null)
            return null;

        // Verify user can access this task (is a mechanic assigned or admin of garage)
        var hasAccess = await _context.RepairTaskAssignments
            .AnyAsync(a => a.RepairTaskId == request.TaskId && a.MechanicId == request.UserId, cancellationToken)
            ||
            await _context.Users
                .AnyAsync(u => u.Id == request.UserId && u.GarageId == task.GarageId, cancellationToken);

        if (!hasAccess)
            return null;

        var mechanics = task.Assignments
            .Select(a => new AssignedMechanicDto(
                a.MechanicId,
                $"{a.Mechanic.FirstName} {a.Mechanic.LastName}",
                a.StartedWorkAt,
                a.CompletedWorkAt
            ))
            .ToList();

        return new RepairTaskDetailsDto(
            task.Id,
            task.TaskTitle,
            task.Description,
            task.Status.ToString(),
            $"{task.Appointment.Client.FirstName} {task.Appointment.Client.LastName}",
            $"{task.Appointment.Vehicle.Brand} {task.Appointment.Vehicle.Model}",
            task.AssignedAt,
            task.StartedAt,
            task.CompletedAt,
            task.CompletionNotes,
            mechanics,
            task.EstimatedMinutes,
            task.ActualMinutes
        );
    }
}


