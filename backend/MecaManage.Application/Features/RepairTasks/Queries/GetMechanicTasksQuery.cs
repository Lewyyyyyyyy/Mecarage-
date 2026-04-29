using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.RepairTasks.Queries;

public record GetMechanicTasksQuery(
    Guid MechanicId
) : IRequest<List<MechanicTaskDto>>;

public record MechanicTaskDto(
    Guid Id,
    string TaskTitle,
    string Description,
    string ClientName,
    string VehicleInfo,
    string Status,
    DateTime AssignedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int? EstimatedMinutes
);

public class GetMechanicTasksQueryHandler : IRequestHandler<GetMechanicTasksQuery, List<MechanicTaskDto>>
{
    private readonly IApplicationDbContext _context;

    public GetMechanicTasksQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<MechanicTaskDto>> Handle(GetMechanicTasksQuery request, CancellationToken cancellationToken)
    {
        return await _context.RepairTasks
            .Where(t => t.Assignments.Any(a => a.MechanicId == request.MechanicId))
            .Include(t => t.Appointment)
            .ThenInclude(a => a.Client)
            .Include(t => t.Appointment)
            .ThenInclude(a => a.Vehicle)
            .OrderByDescending(t => t.AssignedAt)
            .Select(t => new MechanicTaskDto(
                t.Id,
                t.TaskTitle,
                t.Description,
                $"{t.Appointment.Client.FirstName} {t.Appointment.Client.LastName}",
                $"{t.Appointment.Vehicle.Brand} {t.Appointment.Vehicle.Model}",
                t.Status.ToString(),
                t.AssignedAt,
                t.StartedAt,
                t.CompletedAt,
                t.EstimatedMinutes
            ))
            .ToListAsync(cancellationToken);
    }
}

