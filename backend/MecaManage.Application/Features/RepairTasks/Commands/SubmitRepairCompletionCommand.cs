using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Entities;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.RepairTasks.Commands;

public record SubmitRepairCompletionCommand(
    Guid TaskId,
    Guid MechanicId,
    string? CompletionNotes = null,
    string? FileUrl = null
) : IRequest<SubmitRepairCompletionResult>;

public record SubmitRepairCompletionResult(bool Success, string Message);

public class SubmitRepairCompletionCommandHandler : IRequestHandler<SubmitRepairCompletionCommand, SubmitRepairCompletionResult>
{
    private readonly IApplicationDbContext _context;

    public SubmitRepairCompletionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SubmitRepairCompletionResult> Handle(SubmitRepairCompletionCommand request, CancellationToken cancellationToken)
    {
        // ── 1. Load task ──────────────────────────────────────────────────────
        var task = await _context.RepairTasks
            .Include(t => t.Assignments)
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task == null)
            return new SubmitRepairCompletionResult(false, "Tâche introuvable");

        var assignment = task.Assignments.FirstOrDefault(a => a.MechanicId == request.MechanicId);
        if (assignment == null)
            return new SubmitRepairCompletionResult(false, "Vous n'êtes pas assigné à cette tâche");

        // ── 2. Validate: invoice must be client-approved (repair phase) ───────
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.AppointmentId == task.AppointmentId && i.ClientApproved, cancellationToken);

        if (invoice == null)
            return new SubmitRepairCompletionResult(false, "Le devis n'a pas encore été approuvé par le client");

        // ── 3. Validate task state ─────────────────────────────────────────────
        if (task.Status == RepairTaskStatus.Fixed)
            return new SubmitRepairCompletionResult(false, "La réparation a déjà été soumise au chef pour validation");

        if (task.Status == RepairTaskStatus.Done || task.Status == RepairTaskStatus.Cancelled)
            return new SubmitRepairCompletionResult(false, "Cette tâche est déjà terminée ou annulée");

        // ── 4. Mark task as Fixed (submitted to chef) ─────────────────────────
        task.Status = RepairTaskStatus.Fixed;
        if (task.StartedAt == null) task.StartedAt = DateTime.UtcNow;
        task.CompletionNotes = request.CompletionNotes;
        _context.RepairTasks.Update(task);

        // ── 5. Update assignment ───────────────────────────────────────────────
        if (assignment.StartedWorkAt == null)
            assignment.StartedWorkAt = DateTime.UtcNow;
        assignment.CompletedWorkAt = DateTime.UtcNow;
        if (request.CompletionNotes != null)
            assignment.MechanicNotes = request.CompletionNotes;
        if (request.FileUrl != null)
            assignment.ExaminationFileUrl = request.FileUrl;
        _context.RepairTaskAssignments.Update(assignment);

        // ── 6. Notify chef ─────────────────────────────────────────────────────
        _context.Notifications.Add(new Notification
        {
            RecipientId      = task.AssignedByChefId,
            RepairTaskId     = task.Id,
            InvoiceId        = invoice.Id,
            Title            = "🔧 Réparation terminée — Validation requise",
            Message          = $"Le mécanicien a terminé la réparation de « {task.TaskTitle} ». " +
                               $"Veuillez valider et notifier le client pour la récupération du véhicule." +
                               (request.CompletionNotes != null ? $" Notes : {request.CompletionNotes}" : ""),
            NotificationType = "RepairCompletedByMechanic",
            CreatedAt        = DateTime.UtcNow,
            IsRead           = false
        });

        // ── 7. Advance intervention tracker ───────────────────────────────
        var intervention = await _context.Interventions
            .FirstOrDefaultAsync(i => i.AppointmentId == task.AppointmentId, cancellationToken);
        if (intervention != null)
        {
            if (request.CompletionNotes != null) intervention.RepairNotes = request.CompletionNotes;
            intervention.Status = InterventionLifecycleStatus.RepairCompleted;
            _context.Interventions.Update(intervention);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new SubmitRepairCompletionResult(true, "Réparation soumise au chef pour validation");
    }
}

