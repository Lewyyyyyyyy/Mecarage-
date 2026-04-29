using MecaManage.Application.Features.Vehicles.Commands;
using MecaManage.Application.Features.Vehicles.Queries;
using MecaManage.API.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MecaManage.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VehiclesController : ControllerBase
{
    private readonly IMediator _mediator;

    public VehiclesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyVehicles()
    {
        var clientId = User.GetUserId();
        if (clientId == Guid.Empty)
            return Unauthorized(new { message = "Invalid user ID" });

        var result = await _mediator.Send(new GetVehiclesQuery(clientId));
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVehicleCommand command)
    {
        var clientId = User.GetUserId();
        if (clientId == Guid.Empty)
            return Unauthorized(new { message = "Invalid user ID" });

        var commandWithClientId = command with { ClientId = clientId };
        var result = await _mediator.Send(commandWithClientId);
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message, vehicleId = result.VehicleId });
    }
}