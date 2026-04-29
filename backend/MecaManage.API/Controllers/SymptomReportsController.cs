using MecaManage.Application.Features.SymptomReports.Commands;
using MecaManage.Application.Features.SymptomReports.Queries;
using MecaManage.API.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MecaManage.API.Controllers;

/// <summary>
/// Manages symptom reports submitted by clients.
/// Clients submit symptoms, AI diagnoses, and Chef reviews and adds professional feedback.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SymptomReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SymptomReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new symptom report from a client.
    /// </summary>
    /// <param name="command">The symptom report creation command.</param>
    /// <returns>Success message with the newly created report ID.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/symptomreports
    ///     {
    ///       "vehicleId": "550e8400-e29b-41d4-a716-446655440000",
    ///       "symptomsDescription": "Le moteur fait un bruit étrange au démarrage..."
    ///     }
    ///
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = "Client")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateSymptomReport([FromBody] CreateSymptomReportDto command)
    {
        var clientId = User.GetUserId();
        var commandWithClient = new CreateSymptomReportCommand(
            clientId,
            command.VehicleId,
            command.SymptomsDescription,
            command.GarageId,
            command.ChefAtelierId
        );

        var result = await _mediator.Send(commandWithClient);
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message, reportId = result.ReportId });
    }

    /// <summary>
    /// Gets all symptom reports for the authenticated client.
    /// </summary>
    /// <returns>List of client's symptom reports.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/symptomreports/my-reports
    ///
    /// </remarks>
    [HttpGet("my-reports")]
    [Authorize(Roles = "Client")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMySymptomReports()
    {
        var clientId = User.GetUserId();
        var result = await _mediator.Send(new GetClientSymptomReportsQuery(clientId));
        return Ok(result);
    }

    /// <summary>
    /// Gets pending symptom reports for chef review in a specific garage.
    /// </summary>
    /// <param name="garageId">The garage ID where the chef works.</param>
    /// <returns>List of pending symptom reports awaiting chef review.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/symptomreports/pending-reviews/880e8400-e29b-41d4-a716-446655440000
    ///
    /// </remarks>
    [HttpGet("pending-reviews/{garageId}")]
    [Authorize(Roles = "ChefAtelier,AdminEntreprise")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingReviews(Guid garageId)
    {
        var chefId = User.GetUserId();
        var result = await _mediator.Send(new GetChefPendingReviewsQuery(chefId, garageId));
        return Ok(result);
    }

    /// <summary>
    /// Adds professional feedback to a symptom report and updates its status.
    /// </summary>
    /// <param name="reportId">The symptom report ID.</param>
    /// <param name="command">The feedback command.</param>
    /// <returns>Success message.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     PATCH /api/symptomreports/550e8400-e29b-41d4-a716-446655440000/feedback
    ///     {
    ///       "feedback": "Le diagnostic d'IA indique un problème de démarreur. C'est confirmé par nos tests...",
    ///       "newStatus": "Approved"
    ///     }
    ///
    /// </remarks>
    [HttpPatch("{reportId}/feedback")]
    [Authorize(Roles = "ChefAtelier,AdminEntreprise")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddChefFeedback(Guid reportId, [FromBody] AddChefFeedbackDto feedbackDto)
    {
        var chefId = User.GetUserId();
        var command = new AddChefFeedbackCommand(
            reportId,
            chefId,
            feedbackDto.Feedback,
            Enum.Parse<Domain.Enums.SymptomReportStatus>(feedbackDto.NewStatus),
            feedbackDto.AvailablePeriodStart,
            feedbackDto.AvailablePeriodEnd
        );

        var result = await _mediator.Send(command);
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }
}

public record AddChefFeedbackDto(
    string Feedback,
    string NewStatus,
    DateTime? AvailablePeriodStart = null,
    DateTime? AvailablePeriodEnd = null
);

public record CreateSymptomReportDto(
    Guid VehicleId,
    string SymptomsDescription,
    Guid? GarageId = null,
    Guid? ChefAtelierId = null
);

