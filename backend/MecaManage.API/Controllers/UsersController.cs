using MecaManage.Application.Features.Users.Commands;
using MecaManage.Application.Features.Users.Queries;
using MecaManage.API.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MecaManage.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetAllUsers()
    {
        var result = await _mediator.Send(new GetUsersQuery());
        return Ok(result);
    }

    [HttpPost("create-staff")]
    [Authorize(Roles = "SuperAdmin,AdminEntreprise,ChefAtelier")]
    public async Task<IActionResult> CreateStaff([FromBody] CreateStaffCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message, userId = result.UserId });
    }

    [HttpGet("garage/{garageId}/staff")]
    [Authorize(Roles = "SuperAdmin,AdminEntreprise,ChefAtelier")]
    public async Task<IActionResult> GetGarageStaff(Guid garageId)
    {
        var result = await _mediator.Send(new GetGarageStaffQuery(garageId));
        return Ok(result);
    }

    [HttpDelete("{userId}")]
    [Authorize(Roles = "SuperAdmin,AdminEntreprise,ChefAtelier")]
    public async Task<IActionResult> DeleteStaff(Guid userId)
    {
        var result = await _mediator.Send(new DeleteStaffCommand(userId));
        if (!result.Success)
        {
            return result.Message == "Utilisateur introuvable"
                ? NotFound(new { message = result.Message })
                : BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }

    [HttpPost("assign-to-garage")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> AssignUserToGarage([FromBody] AssignUserToGarageCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }
}

