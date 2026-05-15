using MecaManage.Application.Features.RepairTasks.Commands;
using MecaManage.Application.Features.RepairTasks.Queries;
using MecaManage.API.Extensions;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MecaManage.API.Controllers;

/// <summary>
/// Manages repair tasks and mechanic assignments.
/// Chef d'Atelier creates tasks and assigns mechanics.
/// Mechanics update task status through the workflow (Assigned -> Done).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RepairTasksController : ControllerBase
{
    private readonly IMediator _mediator;

    public RepairTasksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new repair task for an approved appointment.
    /// </summary>
    /// <param name="command">The repair task creation command.</param>
    /// <returns>Success message with the newly created task ID.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/repairtasks
    ///     {
    ///       "appointmentId": "550e8400-e29b-41d4-a716-446655440000",
    ///       "taskTitle": "Engine Starter Replacement",
    ///       "description": "Replace faulty starter motor with OEM part",
    ///       "estimatedMinutes": 90,
    ///       "mechanicIds": [
    ///         "660e8400-e29b-41d4-a716-446655440000",
    ///         "770e8400-e29b-41d4-a716-446655440000"
    ///       ]
    ///     }
    ///
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = "ChefAtelier,AdminEntreprise")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateRepairTask([FromBody] CreateRepairTaskCommand command)
    {
        var chefId = User.GetUserId();
        var commandWithChef = new CreateRepairTaskCommand(
            command.AppointmentId,
            chefId,
            command.TaskTitle,
            command.Description,
            command.MechanicIds,
            command.EstimatedMinutes
        );

        var result = await _mediator.Send(commandWithChef);
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message, taskId = result.TaskId });
    }

    /// <summary>
    /// Gets all repair tasks assigned to the authenticated mechanic.
    /// </summary>
    /// <returns>List of mechanic's assigned repair tasks.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/repairtasks/my-tasks
    ///
    /// </remarks>
    [HttpGet("my-tasks")]
    [Authorize(Roles = "Mecanicien")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyTasks()
    {
        var mechanicId = User.GetUserId();
        var result = await _mediator.Send(new GetMechanicTasksQuery(mechanicId));
        return Ok(result);
    }

    /// <summary>
    /// Gets detailed information about a specific repair task.
    /// </summary>
    /// <param name="taskId">The repair task ID.</param>
    /// <returns>Detailed task information with assigned mechanics.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/repairtasks/550e8400-e29b-41d4-a716-446655440000
    ///
    /// </remarks>
    [HttpGet("{taskId}")]
    [Authorize(Roles = "Mecanicien,ChefAtelier")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTaskDetails(Guid taskId)
    {
        var userId = User.GetUserId();
        var result = await _mediator.Send(new GetRepairTaskDetailsQuery(taskId, userId));
        if (result == null)
            return NotFound(new { message = "Task not found or access denied" });
        return Ok(result);
    }

    /// <summary>
    /// Assigns a mechanic to a repair task.
    /// </summary>
    /// <param name="taskId">The repair task ID.</param>
    /// <param name="assignDto">The mechanic assignment details.</param>
    /// <returns>Success message.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/repairtasks/550e8400-e29b-41d4-a716-446655440000/assign-mechanic
    ///     {
    ///       "mechanicId": "660e8400-e29b-41d4-a716-446655440000"
    ///     }
    ///
    /// </remarks>
    [HttpPost("{taskId}/assign-mechanic")]
    [Authorize(Roles = "ChefAtelier,AdminEntreprise")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AssignMechanic(Guid taskId, [FromBody] AssignMechanicDto assignDto)
    {
        var chefId = User.GetUserId();
        var result = await _mediator.Send(new AssignMechanicCommand(taskId, assignDto.MechanicId, chefId));
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Updates the status of a repair task.
    /// Mechanics must follow the state machine: Assigned -> InProgress -> Fixed -> Tested -> Done
    /// </summary>
    /// <param name="taskId">The repair task ID.</param>
    /// <param name="statusUpdateDto">The status update details.</param>
    /// <returns>Success message.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     PATCH /api/repairtasks/550e8400-e29b-41d4-a716-446655440000/status
    ///     {
    ///       "newStatus": "InProgress"
    ///     }
    ///
    /// Or when completing:
    ///
    ///     PATCH /api/repairtasks/550e8400-e29b-41d4-a716-446655440000/status
    ///     {
    ///       "newStatus": "Done",
    ///       "completionNotes": "Starter successfully replaced and tested. Vehicle starts normally."
    ///     }
    ///
    /// </remarks>
    [HttpPatch("{taskId}/status")]
    [Authorize(Roles = "Mecanicien")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateTaskStatus(Guid taskId, [FromBody] UpdateTaskStatusDto statusUpdateDto)
    {
        var mechanicId = User.GetUserId();
        var command = new UpdateRepairTaskStatusCommand(
            taskId,
            mechanicId,
            Enum.Parse<RepairTaskStatus>(statusUpdateDto.NewStatus),
            statusUpdateDto.CompletionNotes
        );

        var result = await _mediator.Send(command);
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }

    [HttpPost("{taskId}/examination-report")]
    [Authorize(Roles = "Mecanicien")]
    public async Task<IActionResult> SubmitExaminationReport(Guid taskId, [FromBody] SubmitExaminationDto dto)
    {
        var mechanicId = User.GetUserId();
        var parts = dto.PartsNeeded.Select(p => new ExaminationPartDto(p.Name, p.Quantity, p.EstimatedPrice)).ToList();
        var result = await _mediator.Send(new SubmitMechanicExaminationReportCommand(taskId, mechanicId, dto.ExaminationObservations, parts, dto.FileUrl));
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Uploads an examination file (photo/document) for a repair task.
    /// Returns the URL to the uploaded file.
    /// </summary>
    [HttpPost("{taskId}/upload-exam-file")]
    [Authorize(Roles = "Mecanicien")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadExaminationFile(Guid taskId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Aucun fichier fourni" });

        // Max 10 MB
        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { message = "Le fichier ne peut pas dépasser 10 Mo" });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "application/pdf" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return BadRequest(new { message = "Type de fichier non autorisé. Utilisez JPEG, PNG, WebP ou PDF." });

        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "examinations");
        Directory.CreateDirectory(uploadsPath);

        var ext = Path.GetExtension(file.FileName).ToLower();
        var fileName = $"exam-{taskId}-{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsPath, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        var request = HttpContext.Request;
        var fileUrl = $"{request.Scheme}://{request.Host}/uploads/examinations/{fileName}";

        return Ok(new { fileUrl });
    }

    [HttpPatch("{taskId}/review-examination")]
    [Authorize(Roles = "ChefAtelier")]
    public async Task<IActionResult> ReviewExamination(Guid taskId, [FromBody] ReviewExaminationDto dto)
    {
        var chefId = User.GetUserId();
        var updatedParts = dto.UpdatedParts?.Select(p =>
            new MecaManage.Application.Features.RepairTasks.Commands.ReviewPartInputDto(p.SparePartId, p.Name, p.Quantity, p.UnitPrice)).ToList();        var result = await _mediator.Send(new ReviewMechanicExaminationCommand(
            taskId, chefId, dto.IsApproved, dto.ServiceFee, dto.DeclineReason,
            dto.UpdatedObservations, updatedParts));
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message, invoiceId = result.InvoiceId });
    }

    /// <summary>
    /// Chef marks a repair task as Tested after successfully testing the repaired vehicle.
    /// This triggers the final quality confirmation step before notifying the client.
    /// </summary>
    [HttpPatch("{taskId}/mark-tested")]
    [Authorize(Roles = "ChefAtelier,AdminEntreprise")]
    public async Task<IActionResult> MarkTaskTested(Guid taskId)
    {
        var chefId = User.GetUserId();
        var result = await _mediator.Send(new MarkTaskTestedCommand(taskId, chefId));
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }

    [HttpGet("pending-examinations/{garageId}")]
    [Authorize(Roles = "ChefAtelier")]
    public async Task<IActionResult> GetPendingExaminations(Guid garageId)
    {
        var chefId = User.GetUserId();
        var result = await _mediator.Send(new GetPendingExaminationsQuery(chefId, garageId));
        return Ok(result);
    }

    /// <summary>
    /// Gets ALL examination reports for a garage (all statuses) — newest first.
    /// </summary>
    [HttpGet("all-examinations/{garageId}")]
    [Authorize(Roles = "ChefAtelier,AdminEntreprise")]
    public async Task<IActionResult> GetAllExaminations(Guid garageId)
    {
        var chefId = User.GetUserId();
        var result = await _mediator.Send(new GetAllExaminationsQuery(chefId, garageId));
        return Ok(result);
    }

    /// <summary>
    /// Gets all repair tasks with client-approved invoices — for chef to manage repairs.
    /// </summary>
    [HttpGet("repair-ready/{garageId}")]
    [Authorize(Roles = "ChefAtelier,AdminEntreprise")]
    public async Task<IActionResult> GetRepairReadyTasks(Guid garageId)
    {
        var chefId = User.GetUserId();
        var result = await _mediator.Send(new GetRepairReadyTasksQuery(chefId, garageId));
        return Ok(result);
    }

    /// <summary>
    /// Unified mechanic update: sets status, writes notes, attaches file, lists parts used (with garage stock autocomplete).
    /// </summary>
    [HttpPatch("{taskId}/mechanic-update")]
    [Authorize(Roles = "Mecanicien")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MechanicUpdate(Guid taskId, [FromBody] MechanicUpdateDto dto)
    {
        var mechanicId = User.GetUserId();
        var parts = dto.Parts?.Select(p => new TaskPartDto(p.SparePartId, p.Name, p.Quantity, p.UnitPrice)).ToList();
        var result = await _mediator.Send(new UpdateMechanicTaskCommand(
            taskId,
            mechanicId,
            dto.SubmitToChef,
            dto.MechanicNotes,
            dto.FileUrl,
            parts
        ));
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }

    [HttpPatch("{taskId}/ready-for-pickup")]
    [Authorize(Roles = "ChefAtelier,AdminEntreprise")]
    public async Task<IActionResult> SignalReadyForPickup(Guid taskId)
    {
        var chefId = User.GetUserId();
        var result = await _mediator.Send(new SignalReadyForPickupCommand(taskId, chefId));
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Mechanic submits repair completion to chef for validation (one-step, post-invoice-approval).
    /// Sets task status to Fixed and notifies the chef.
    /// </summary>
    [HttpPatch("{taskId}/submit-repair")]
    [Authorize(Roles = "Mecanicien")]
    public async Task<IActionResult> SubmitRepairCompletion(Guid taskId, [FromBody] SubmitRepairDto dto)
    {
        var mechanicId = User.GetUserId();
        var result = await _mediator.Send(new SubmitRepairCompletionCommand(taskId, mechanicId, dto.CompletionNotes, dto.FileUrl));
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }
}

public record AssignMechanicDto(
    Guid MechanicId
);

public record UpdateTaskStatusDto(
    string NewStatus,
    string? CompletionNotes = null
);

public record SubmitExaminationDto(
    string ExaminationObservations,
    List<ExaminationPartInputDto> PartsNeeded,
    string? FileUrl = null
);

public record ExaminationPartInputDto(
    string Name,
    int Quantity,
    decimal EstimatedPrice
);

public record ReviewExaminationDto(
    bool IsApproved,
    decimal ServiceFee,
    string? DeclineReason = null,
    string? UpdatedObservations = null,
    List<ReviewPartInputDto>? UpdatedParts = null
);

public record ReviewPartInputDto(
    Guid? SparePartId,
    string Name,
    int Quantity,
    decimal UnitPrice
);

public record MechanicUpdateDto(
    bool SubmitToChef = false,
    string? MechanicNotes = null,
    string? FileUrl = null,
    List<TaskPartInputDto>? Parts = null
);

public record TaskPartInputDto(
    Guid SparePartId,
    string Name,
    int Quantity,
    decimal UnitPrice
);

public record SubmitRepairDto(
    string? CompletionNotes = null,
    string? FileUrl = null
);

