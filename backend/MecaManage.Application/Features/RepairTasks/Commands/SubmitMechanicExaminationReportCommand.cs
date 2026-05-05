using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Entities;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.RepairTasks.Commands;

public record ExaminationPartDto(
    string Name,
    int Quantity,
    decimal EstimatedPrice
);

public record SubmitMechanicExaminationReportCommand(
    Guid RepairTaskId,
    Guid MechanicId,
    string ExaminationObservations,
    List<ExaminationPartDto> PartsNeeded,
    string? FileUrl = null
) : IRequest<SubmitExaminationResult>;

public record SubmitExaminationResult(bool Success, string Message);

public class SubmitMechanicExaminationReportCommandHandler : IRequestHandler<SubmitMechanicExaminationReportCommand, SubmitExaminationResult>
{
    private readonly IApplicationDbContext _context;

    public SubmitMechanicExaminationReportCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SubmitExaminationResult> Handle(SubmitMechanicExaminationReportCommand request, CancellationToken cancellationToken)
    {
        // Find the assignment for this mechanic on this task
        var assignment = await _context.RepairTaskAssignments
            .Include(a => a.RepairTask)
                .ThenInclude(t => t.Appointment)
            .FirstOrDefaultAsync(a => a.RepairTaskId == request.RepairTaskId && a.MechanicId == request.MechanicId, cancellationToken);

        if (assignment == null)
            return new SubmitExaminationResult(false, "Tâche non trouvée ou vous n'êtes pas assigné à cette tâche");

        if (assignment.ExaminationStatus != "None")
            return new SubmitExaminationResult(false, "Un rapport d'examen a déjà été soumis pour cette tâche");

        // Serialize parts to JSON
        var partsJson = System.Text.Json.JsonSerializer.Serialize(request.PartsNeeded);

        assignment.ExaminationObservations = request.ExaminationObservations;
        assignment.PartsNeeded = partsJson;
        assignment.ExaminationFileUrl = request.FileUrl;
        assignment.ExaminationSubmittedAt = DateTime.UtcNow;
        assignment.ExaminationStatus = "Pending";

        _context.RepairTaskAssignments.Update(assignment);

        // Notify the chef atelier
        var repairTask = assignment.RepairTask;
        var chefNotification = new Notification
        {
            RecipientId = repairTask.AssignedByChefId,
            RepairTaskId = repairTask.Id,
            Title = "Rapport d'examen soumis",
            Message = $"Le mécanicien a soumis son rapport d'examen pour la tâche: {repairTask.TaskTitle}. Veuillez examiner et approuver.",
            NotificationType = "ExaminationReportReady",
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.Notifications.Add(chefNotification);

        // ── Advance intervention to UnderExamination ─────────────────────
        var appointmentId = repairTask.AppointmentId;
        var intervention = await _context.Interventions
            .FirstOrDefaultAsync(i => i.AppointmentId == appointmentId, cancellationToken);
        if (intervention != null &&
            intervention.Status == InterventionLifecycleStatus.Created)
        {
            intervention.Status = InterventionLifecycleStatus.UnderExamination;
            _context.Interventions.Update(intervention);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new SubmitExaminationResult(true, "Rapport d'examen soumis avec succès");
    }
}

