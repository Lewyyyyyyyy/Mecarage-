using MecaManage.Application.Features.Interventions.Commands;
using MecaManage.Application.Features.Interventions.Queries;
using MecaManage.Application.Features.InterventionLifecycle.Commands;
using MecaManage.Application.Features.InterventionLifecycle.Queries;
using MecaManage.API.Extensions;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MecaManage.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InterventionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public InterventionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyInterventions()
    {
        var result = await _mediator.Send(new GetInterventionsQuery());
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInterventionRequest request)
    {
        var clientId = User.GetUserId();
        var command = new CreateInterventionCommand(
            clientId,
            request.VehicleId, request.GarageId,
            request.Description, request.UrgencyLevel,
            request.AppointmentDate
        );
        var result = await _mediator.Send(command);
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message, interventionId = result.InterventionId });
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "SuperAdmin,AdminEntreprise,ChefAtelier")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        var command = new UpdateInterventionStatusCommand(id, request.NewStatus, request.Notes);
        var result = await _mediator.Send(command);
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }

    [HttpPut("{id}/assign")]
    [Authorize(Roles = "SuperAdmin,AdminEntreprise,ChefAtelier")]
    public async Task<IActionResult> AssignMecanicien(Guid id, [FromBody] AssignRequest request)
    {
        var command = new AssignMecanicienCommand(id, request.MecanicienId);
        var result = await _mediator.Send(command);
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }

    [HttpPost("{id}/diagnose")]
    [Authorize(Roles = "SuperAdmin,AdminEntreprise,ChefAtelier")]
    public async Task<IActionResult> Diagnose(Guid id)
    {
        var command = new DiagnoseInterventionCommand(id);
        var result = await _mediator.Send(command);
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Intervention LIFECYCLE endpoints  (new — side tracker)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>GET all interventions for a garage (chef / admin view)</summary>
    [HttpGet("lifecycle/garage/{garageId}")]
    [Authorize(Roles = "SuperAdmin,AdminEntreprise,ChefAtelier")]
    public async Task<IActionResult> GetGarageInterventions(Guid garageId)
    {
        var result = await _mediator.Send(new GetGarageInterventionsQuery(garageId));
        return Ok(result);
    }

    /// <summary>GET my interventions as a client</summary>
    [HttpGet("lifecycle/my")]
    public async Task<IActionResult> GetMyInterventionLifecycles()
    {
        var clientId = User.GetUserId();
        var result   = await _mediator.Send(new GetClientInterventionsQuery(clientId));
        return Ok(result);
    }

    /// <summary>GET intervention detail by id</summary>
    [HttpGet("lifecycle/{id:guid}")]
    public async Task<IActionResult> GetInterventionDetail(Guid id)
    {
        var result = await _mediator.Send(new GetInterventionDetailQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>GET intervention linked to an appointment</summary>
    [HttpGet("lifecycle/by-appointment/{appointmentId:guid}")]
    public async Task<IActionResult> GetByAppointment(Guid appointmentId)
    {
        var result = await _mediator.Send(new GetInterventionByAppointmentQuery(appointmentId));
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>PATCH register payment — transitions to Closed (admin only)</summary>
    [HttpPatch("lifecycle/{id:guid}/payment")]
    [Authorize(Roles = "SuperAdmin,AdminEntreprise,ChefAtelier")]
    public async Task<IActionResult> RegisterPayment(Guid id, [FromBody] RegisterPaymentRequest req)
    {
        var paidBy = User.GetUserFullName();
        var command = new RegisterPaymentCommand(id, req.PaymentAmount, req.PaymentMethod, paidBy);
        var result  = await _mediator.Send(command);
        if (!result.Success) return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }
}

public class CreateInterventionRequest
{
    public Guid VehicleId { get; set; }
    public Guid GarageId { get; set; }
    public string Description { get; set; } = string.Empty;
    public UrgencyLevel UrgencyLevel { get; set; }
    public DateTime? AppointmentDate { get; set; }
}

public class UpdateStatusRequest
{
    public InterventionStatus NewStatus { get; set; }
    public string? Notes { get; set; }
}

public class AssignRequest
{
    public Guid MecanicienId { get; set; }
}

public class RegisterPaymentRequest
{
    public decimal PaymentAmount { get; set; }
    public string  PaymentMethod { get; set; } = "Cash";
}
