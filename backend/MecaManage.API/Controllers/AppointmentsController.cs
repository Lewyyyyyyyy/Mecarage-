using MecaManage.Application.Features.Appointments.Commands;
using MecaManage.Application.Features.Appointments.Queries;
using MecaManage.API.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MecaManage.API.Controllers;

/// <summary>
/// Manages service appointment bookings at garages.
/// Clients book appointments, Chef d'Atelier (Chef) approves or declines them.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AppointmentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new appointment booking request.
    /// </summary>
    /// <param name="command">The appointment creation command.</param>
    /// <returns>Success message with the newly created appointment ID.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/appointments
    ///     {
    ///       "vehicleId": "550e8400-e29b-41d4-a716-446655440000",
    ///       "garageId": "660e8400-e29b-41d4-a716-446655440000",
    ///       "preferredDate": "2026-05-15T00:00:00Z",
    ///       "preferredTime": "09:30:00",
    ///       "symptomReportId": "770e8400-e29b-41d4-a716-446655440000",
    ///       "specialRequests": "Please call before arrival"
    ///     }
    ///
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = "Client")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentCommand command)
    {
        var clientId = User.GetUserId();
        var commandWithClient = new CreateAppointmentCommand(
            clientId,
            command.VehicleId,
            command.GarageId,
            command.PreferredDate,
            command.PreferredTime,
            command.SymptomReportId,
            command.SpecialRequests
        );

        var result = await _mediator.Send(commandWithClient);
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message, appointmentId = result.AppointmentId });
    }

    /// <summary>
    /// Gets all appointments for the authenticated client.
    /// </summary>
    /// <returns>List of client's appointments.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/appointments/my-appointments
    ///
    /// </remarks>
    [HttpGet("my-appointments")]
    [Authorize(Roles = "Client")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyAppointments()
    {
        var clientId = User.GetUserId();
        var result = await _mediator.Send(new GetClientAppointmentsQuery(clientId));
        return Ok(result);
    }

    /// <summary>
    /// Gets pending appointment requests for a chef's garage.
    /// </summary>
    /// <param name="garageId">The garage ID.</param>
    /// <returns>List of pending appointments awaiting chef approval.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/appointments/pending/880e8400-e29b-41d4-a716-446655440000
    ///
    /// </remarks>
    [HttpGet("pending/{garageId}")]
    [Authorize(Roles = "ChefAtelier,AdminEntreprise")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingAppointments(Guid garageId)
    {
        var chefId = User.GetUserId();
        var result = await _mediator.Send(new GetChefPendingAppointmentsQuery(chefId, garageId));
        return Ok(result);
    }

    /// <summary>
    /// Gets ALL appointments for a garage (all statuses) for traceability — newest first.
    /// </summary>
    [HttpGet("garage/{garageId}")]
    [Authorize(Roles = "ChefAtelier,AdminEntreprise")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGarageAppointments(Guid garageId)
    {
        var chefId = User.GetUserId();
        var result = await _mediator.Send(new GetGarageAppointmentsQuery(chefId, garageId));
        return Ok(result);
    }

    /// <summary>
    /// Approves a pending appointment request.
    /// </summary>
    /// <param name="appointmentId">The appointment ID to approve.</param>
    /// <returns>Success message.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     PATCH /api/appointments/550e8400-e29b-41d4-a716-446655440000/approve
    ///
    /// </remarks>
    [HttpPatch("{appointmentId}/approve")]
    [Authorize(Roles = "ChefAtelier,AdminEntreprise")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ApproveAppointment(Guid appointmentId)
    {
        var chefId = User.GetUserId();
        var result = await _mediator.Send(new ApproveAppointmentCommand(appointmentId, chefId));
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Declines a pending appointment request with a reason.
    /// </summary>
    /// <param name="appointmentId">The appointment ID to decline.</param>
    /// <param name="declineDto">The decline details.</param>
    /// <returns>Success message.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     PATCH /api/appointments/550e8400-e29b-41d4-a716-446655440000/decline
    ///     {
    ///       "declineReason": "Workshop fully booked for that date"
    ///     }
    ///
    /// </remarks>
    [HttpPatch("{appointmentId}/decline")]
    [Authorize(Roles = "ChefAtelier,AdminEntreprise")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeclineAppointment(Guid appointmentId, [FromBody] DeclineAppointmentDto declineDto)
    {
        var chefId = User.GetUserId();
        var result = await _mediator.Send(new DeclineAppointmentCommand(appointmentId, chefId, declineDto.DeclineReason));
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }
}

public record DeclineAppointmentDto(
    string DeclineReason
);

