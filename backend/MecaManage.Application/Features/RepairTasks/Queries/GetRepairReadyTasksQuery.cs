using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.RepairTasks.Queries;

public record GetRepairReadyTasksQuery(Guid ChefId, Guid GarageId) : IRequest<List<RepairReadyTaskDto>>;

public record RepairReadyTaskDto(
    Guid TaskId,
    string TaskTitle,
    string Description,
    string ClientName,
    string VehicleInfo,
    string TaskStatus,
    string InvoiceStatus,
    decimal InvoiceTotal,
    string InvoiceNumber,
    DateTime AppointmentDate,
    List<string> AssignedMechanics,
    DateTime InvoiceApprovedAt
);

public class GetRepairReadyTasksQueryHandler : IRequestHandler<GetRepairReadyTasksQuery, List<RepairReadyTaskDto>>
{
    private readonly IApplicationDbContext _context;

    public GetRepairReadyTasksQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RepairReadyTaskDto>> Handle(GetRepairReadyTasksQuery request, CancellationToken cancellationToken)
    {
        // Find repair tasks where the linked invoice has been approved by the client
        var tasks = await _context.RepairTasks
            .Where(t => t.GarageId == request.GarageId && t.AssignedByChefId == request.ChefId)
            .Include(t => t.Appointment)
                .ThenInclude(a => a.Client)
            .Include(t => t.Appointment)
                .ThenInclude(a => a.Vehicle)
            .Include(t => t.Appointment)
                .ThenInclude(a => a.Invoice)
            .Include(t => t.Assignments)
                .ThenInclude(a => a.Mechanic)
            .Where(t => t.Appointment.Invoice != null && t.Appointment.Invoice.ClientApproved
                        && t.Status != RepairTaskStatus.Done && t.Status != RepairTaskStatus.Cancelled)
            .OrderByDescending(t => t.Appointment.Invoice!.ClientApprovedAt)
            .ToListAsync(cancellationToken);

        return tasks.Select(t =>
        {
            var inv = t.Appointment.Invoice!;
            var client = t.Appointment.Client;
            var vehicle = t.Appointment.Vehicle;
            var mechanics = t.Assignments
                .Select(a => $"{a.Mechanic.FirstName} {a.Mechanic.LastName}")
                .ToList();

            return new RepairReadyTaskDto(
                t.Id,
                t.TaskTitle,
                t.Description,
                $"{client.FirstName} {client.LastName}",
                $"{vehicle.Brand} {vehicle.Model} ({vehicle.Year})",
                t.Status.ToString(),
                inv.Status.ToString(),
                inv.TotalAmount,
                inv.InvoiceNumber,
                t.Appointment.PreferredDate,
                mechanics,
                inv.ClientApprovedAt ?? DateTime.UtcNow
            );
        }).ToList();
    }
}

