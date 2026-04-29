using MecaManage.Application.Features.Interventions.Commands;
using MecaManage.Application.Features.Interventions.Queries;
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