using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Entities;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.RepairTasks.Commands;

public record CreateRepairTaskCommand(
    Guid AppointmentId,
    Guid ChefId,
    string TaskTitle,
    string Description,
    List<Guid>? MechanicIds = null,
    int? EstimatedMinutes = null
) : IRequest<CreateRepairTaskResult>;

public record CreateRepairTaskResult(bool Success, string Message, Guid? TaskId);

public class CreateRepairTaskCommandHandler : IRequestHandler<CreateRepairTaskCommand, CreateRepairTaskResult>
{
    private readonly IApplicationDbContext _context;

    public CreateRepairTaskCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CreateRepairTaskResult> Handle(CreateRepairTaskCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Garage)
            .Include(a => a.Client)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);

        if (appointment == null)
            return new CreateRepairTaskResult(false, "Rendez-vous introuvable", null);

        if (appointment.Status != AppointmentStatus.Approved)
            return new CreateRepairTaskResult(false, "Seuls les rendez-vous approuvés peuvent avoir des tâches", null);

        // Verify invoice is approved if one already exists (exam tasks can be created before any invoice)
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.AppointmentId == request.AppointmentId, cancellationToken);

        if (invoice != null && !invoice.ClientApproved)
            return new CreateRepairTaskResult(false, "La facture doit être approuvée par le client avant de créer des tâches de réparation", null);

        // Verify chef belongs to garage
        var chefBelongsToGarage = await _context.Users
            .AnyAsync(u => u.Id == request.ChefId && u.GarageId == appointment.GarageId && u.Role == UserRole.ChefAtelier, cancellationToken);

        if (!chefBelongsToGarage)
            return new CreateRepairTaskResult(false, "Vous n'avez pas les permissions pour créer une tâche", null);

        var task = new RepairTask
        {
            AppointmentId = request.AppointmentId,
            GarageId = appointment.GarageId,
            TenantId = appointment.Garage.TenantId,
            AssignedByChefId = request.ChefId,
            TaskTitle = request.TaskTitle,
            Description = request.Description,
            Status = RepairTaskStatus.Assigned,
            AssignedAt = DateTime.UtcNow,
            EstimatedMinutes = request.EstimatedMinutes
        };

        // Assign mechanics if provided
        if (request.MechanicIds != null && request.MechanicIds.Count > 0)
        {
            // Verify all mechanics belong to the garage
            var invalidMechanics = 0;
            foreach (var mechanicId in request.MechanicIds)
            {
                var mechanicExists = await _context.Users
                    .AnyAsync(u => u.Id == mechanicId && u.GarageId == appointment.GarageId && u.Role == UserRole.Mecanicien, cancellationToken);

                if (!mechanicExists)
                    invalidMechanics++;
                else
                {
                    var assignment = new RepairTaskAssignment
                    {
                        MechanicId = mechanicId,
                        AssignedAt = DateTime.UtcNow
                    };
                    task.Assignments.Add(assignment);
                }
            }

            if (invalidMechanics > 0)
                return new CreateRepairTaskResult(false, $"{invalidMechanics} mécanicien(s) n'appartiennent pas à ce garage", null);
        }

        _context.RepairTasks.Add(task);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateRepairTaskResult(true, "Tâche de réparation créée avec succès", task.Id);
    }
}

