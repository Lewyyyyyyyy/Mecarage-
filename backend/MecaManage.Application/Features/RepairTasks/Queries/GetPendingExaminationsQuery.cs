using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MecaManage.Application.Features.RepairTasks.Queries;

public record GetPendingExaminationsQuery(Guid ChefId, Guid GarageId) : IRequest<List<PendingExaminationDto>>;

public record ExaminationPartItemDto(string Name, int Quantity, decimal EstimatedPrice, Guid? SparePartId = null);

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
                    var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var raw = JsonSerializer.Deserialize<List<JsonElement>>(a.PartsNeeded, opts);
                    if (raw != null)
                    {
                        parts = raw.Select(p =>
                        {
                            string name = TryGetString(p, "name") ?? "";
                            int qty     = TryGetInt(p, "quantity");
                            decimal price = TryGetDecimal(p, "unitPrice")
                                         ?? TryGetDecimal(p, "estimatedPrice")
                                         ?? 0m;
                            // Preserve the sparePartId so the chef can round-trip it
                            Guid? sparePartId = null;
                            var idStr = TryGetString(p, "sparePartId");
                            if (!string.IsNullOrEmpty(idStr) && Guid.TryParse(idStr, out var parsed))
                                sparePartId = parsed;
                            return new ExaminationPartItemDto(name, qty, price, sparePartId);
                        }).ToList();
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

    private static string? TryGetString(JsonElement el, string key)
    {
        foreach (var prop in el.EnumerateObject())
            if (string.Equals(prop.Name, key, StringComparison.OrdinalIgnoreCase))
                return prop.Value.GetString();
        return null;
    }

    private static int TryGetInt(JsonElement el, string key)
    {
        foreach (var prop in el.EnumerateObject())
            if (string.Equals(prop.Name, key, StringComparison.OrdinalIgnoreCase))
                return prop.Value.TryGetInt32(out var v) ? v : 0;
        return 0;
    }

    private static decimal? TryGetDecimal(JsonElement el, string key)
    {
        foreach (var prop in el.EnumerateObject())
            if (string.Equals(prop.Name, key, StringComparison.OrdinalIgnoreCase))
                return prop.Value.TryGetDecimal(out var v) ? v : null;
        return null;
    }
}

