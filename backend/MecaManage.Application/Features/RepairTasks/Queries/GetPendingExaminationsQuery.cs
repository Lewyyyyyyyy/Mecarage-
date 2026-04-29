using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MecaManage.Application.Features.RepairTasks.Queries;

public record GetPendingExaminationsQuery(Guid ChefId, Guid GarageId) : IRequest<List<PendingExaminationDto>>;

public record ExaminationPartItemDto(string Name, int Quantity, decimal EstimatedPrice);

public record PendingExaminationDto(
    Guid RepairTaskId,
    Guid AssignmentId,
    string TaskTitle,
    string ClientName,
    string VehicleInfo,
    string MechanicName,
    string ExaminationObservations,
    List<ExaminationPartItemDto> PartsNeeded,
    DateTime ExaminationSubmittedAt,
    string? FileUrl = null
);

public class GetPendingExaminationsQueryHandler : IRequestHandler<GetPendingExaminationsQuery, List<PendingExaminationDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPendingExaminationsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PendingExaminationDto>> Handle(GetPendingExaminationsQuery request, CancellationToken cancellationToken)
    {
        var assignments = await _context.RepairTaskAssignments
            .Where(a => a.ExaminationStatus == "Pending" && a.RepairTask.GarageId == request.GarageId && a.RepairTask.AssignedByChefId == request.ChefId)
            .Include(a => a.RepairTask)
                .ThenInclude(t => t.Appointment)
                    .ThenInclude(apt => apt.Client)
            .Include(a => a.RepairTask)
                .ThenInclude(t => t.Appointment)
                    .ThenInclude(apt => apt.Vehicle)
            .Include(a => a.Mechanic)
            .OrderByDescending(a => a.ExaminationSubmittedAt)
            .ToListAsync(cancellationToken);

        return assignments.Select(a =>
        {
            List<ExaminationPartItemDto> parts = new();
            if (!string.IsNullOrEmpty(a.PartsNeeded))
            {
                try
                {
                    var raw = JsonSerializer.Deserialize<List<JsonElement>>(a.PartsNeeded);
                    if (raw != null)
                    {
                        parts = raw.Select(p => new ExaminationPartItemDto(
                            p.GetProperty("name").GetString() ?? "",
                            p.GetProperty("quantity").GetInt32(),
                            p.GetProperty("estimatedPrice").GetDecimal()
                        )).ToList();
                    }
                }
                catch { }
            }

            return new PendingExaminationDto(
                a.RepairTaskId,
                a.Id,
                a.RepairTask.TaskTitle,
                $"{a.RepairTask.Appointment.Client.FirstName} {a.RepairTask.Appointment.Client.LastName}",
                $"{a.RepairTask.Appointment.Vehicle.Brand} {a.RepairTask.Appointment.Vehicle.Model}",
                $"{a.Mechanic.FirstName} {a.Mechanic.LastName}",
                a.ExaminationObservations ?? "",
                parts,
                a.ExaminationSubmittedAt ?? DateTime.UtcNow,
                a.ExaminationFileUrl
            );
        }).ToList();
    }
}

