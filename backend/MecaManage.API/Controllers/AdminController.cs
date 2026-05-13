using MecaManage.Application.Features.Admin.Commands;
using MecaManage.Application.Features.Admin.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MecaManage.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Public trust KPIs for landing page (no authentication required)
    /// </summary>
    [HttpGet("public-trust-kpis")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicTrustKpis()
    {
        var kpis = await _mediator.Send(new GetAdminKpisQuery());
        var successRate = kpis.TotalInterventions == 0
            ? 0
            : Math.Round((double)kpis.CompletedInterventions * 100 / kpis.TotalInterventions, 1);

        return Ok(new PublicTrustKpisResponse(
            TotalTenants: kpis.TotalTenants,
            TotalGarages: kpis.TotalGarages,
            TotalInterventions: kpis.TotalInterventions,
            CompletedInterventions: kpis.CompletedInterventions,
            ActiveClients: kpis.ActiveClients,
            SuccessRate: successRate
        ));
    }

    /// <summary>
    /// Get KPIs for SuperAdmin dashboard
    /// </summary>
    [HttpGet("kpis")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetKpis()
    {
        var kpis = await _mediator.Send(new GetAdminKpisQuery());
        return Ok(kpis);
    }

    /// <summary>
    /// Create a garage admin (ChefAtelier) and assign to a garage
    /// </summary>
    [HttpPost("create-garage-admin")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateGarageAdmin([FromBody] CreateGarageAdminCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message, adminId = result.AdminId });
    }

    /// <summary>
    /// Update garage admin assignment
    /// </summary>
    [HttpPut("garage-admin/{garageId}")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateGarageAdmin(Guid garageId, [FromBody] UpdateGarageAdminRequest request)
    {
        var command = new UpdateGarageAdminCommand(garageId, request.NewAdminId);
        var result = await _mediator.Send(command);
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Get all garage admins for a tenant
    /// </summary>
    [HttpGet("garage-admins/{tenantId}")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetGarageAdmins(Guid tenantId)
    {
        var result = await _mediator.Send(new GetGarageAdminsQuery(tenantId));
        return Ok(result);
    }
}

public record UpdateGarageAdminRequest(Guid? NewAdminId);
public record PublicTrustKpisResponse(
    int TotalTenants,
    int TotalGarages,
    int TotalInterventions,
    int CompletedInterventions,
    int ActiveClients,
    double SuccessRate
);

