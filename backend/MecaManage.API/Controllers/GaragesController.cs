using MecaManage.Application.Features.Garages.Commands;
using MecaManage.Application.Features.Garages.Queries;
using MecaManage.API.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MecaManage.API.Controllers;

/// <summary>
/// Manages garage operations. Garages are repair workshops belonging to a tenant.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GaragesController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the GaragesController.
    /// </summary>
    public GaragesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets all garages for the current user's tenant.
    /// </summary>
    /// <returns>List of garages belonging to the user's tenant.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/garages
    ///
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyGarages()
    {
        var tenantId = User.GetTenantId();
        var garageId = User.GetGarageId();

        // ChefAtelier may have GarageId but no TenantId — query by garageId directly
        if (tenantId == Guid.Empty && garageId.HasValue)
        {
            var result = await _mediator.Send(new GetGarageByIdQuery(garageId.Value));
            return Ok(result);
        }

        var garages = await _mediator.Send(new GetGaragesQuery(tenantId));
        return Ok(garages);
    }

    /// <summary>
    /// Gets all garages in the platform (SuperAdmin only).
    /// </summary>
    /// <returns>List of all garages across all tenants.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/garages/all
    ///
    /// </remarks>
    [HttpGet("all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllGarages()
    {
        var result = await _mediator.Send(new GetAllGaragesQuery());
        return Ok(result);
    }

    /// <summary>
    /// Gets all garages for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID (GUID).</param>
    /// <returns>List of garages for the specified tenant.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/garages/tenant/550e8400-e29b-41d4-a716-446655440000
    ///
    /// </remarks>
    [HttpGet("tenant/{tenantId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTenantGarages(Guid tenantId)
    {
        var result = await _mediator.Send(new GetTenantGaragesQuery(tenantId));
        return Ok(result);
    }

    /// <summary>
    /// Creates a new garage.
    /// </summary>
    /// <param name="command">The garage creation command.</param>
    /// <param name="tenantId">Optional tenant ID query parameter (for SuperAdmin creating garage for specific tenant).</param>
    /// <returns>Success message with the newly created garage ID.</returns>
    /// <remarks>
    /// For SuperAdmin: Pass tenantId as query parameter (?tenantId={id})
    /// For AdminEntreprise: tenantId is extracted from JWT claims
    ///
    /// Sample request (SuperAdmin):
    ///
    ///     POST /api/garages?tenantId=550e8400-e29b-41d4-a716-446655440000
    ///     {
    ///       "name": "Central Workshop",
    ///       "address": "456 Central Avenue",
    ///       "city": "Lyon",
    ///       "phone": "+33987654321",
    ///       "latitude": 45.7640,
    ///       "longitude": 4.8357
    ///     }
    ///
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,AdminEntreprise")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateGarageCommand command, [FromQuery] string? tenantId = null)
    {
        Guid resolvedTenantId = Guid.Empty;

        // If tenantId is provided in query (SuperAdmin managing a tenant), use it
        if (!string.IsNullOrEmpty(tenantId) && Guid.TryParse(tenantId, out var queryTenantId))
        {
            resolvedTenantId = queryTenantId;
        }
        else
        {
            // Otherwise, try to get it from JWT claims
            resolvedTenantId = User.GetTenantId();
        }

        if (resolvedTenantId == Guid.Empty)
            return Unauthorized(new { message = "Invalid tenant ID" });

        var commandWithTenant = new CreateGarageCommand(
            command.Name,
            command.Address,
            command.City,
            command.Phone,
            command.Latitude,
            command.Longitude,
            resolvedTenantId
        );

        var result = await _mediator.Send(commandWithTenant);
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message, garageId = result.GarageId });
    }

    /// <summary>
    /// Gets all interventions for a specific garage.
    /// </summary>
    /// <param name="garageId">The garage ID (GUID).</param>
    /// <returns>List of interventions for the garage.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/garages/880e8400-e29b-41d4-a716-446655440000/interventions
    ///
    /// </remarks>
    [HttpGet("{garageId}/interventions")]
    [Authorize(Roles = "AdminEntreprise,ChefAtelier")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetGarageInterventions(Guid garageId)
    {
        var result = await _mediator.Send(new GetGarageInterventionsQuery(garageId));
        return Ok(result);
    }

    /// <summary>
    /// Gets all clients for a specific garage.
    /// </summary>
    /// <param name="garageId">The garage ID (GUID).</param>
    /// <returns>List of clients for the garage.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/garages/880e8400-e29b-41d4-a716-446655440000/clients
    ///
    /// </remarks>
    [HttpGet("{garageId}/clients")]
    [Authorize(Roles = "AdminEntreprise,ChefAtelier")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetGarageClients(Guid garageId)
    {
        var result = await _mediator.Send(new GetGarageClientsQuery(garageId));
        return Ok(result);
    }
}

