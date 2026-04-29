using MecaManage.Application.Features.Tenants.Commands;
using MecaManage.Application.Features.Tenants.Queries;
using MecaManage.API.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MecaManage.API.Controllers;

/// <summary>
/// Manages tenant operations. Tenants represent companies/garage organizations using the platform.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantsController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the TenantsController.
    /// </summary>
    public TenantsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets all tenants on the platform.
    /// </summary>
    /// <returns>List of all tenants with garage and user counts.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/tenants
    ///
    /// </remarks>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin,Client")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetTenantsQuery());
        return Ok(result);
    }

    /// <summary>
    /// Gets a specific tenant by ID.
    /// </summary>
    /// <param name="id">The tenant ID (GUID).</param>
    /// <returns>Tenant details including garage and user counts.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/tenants/550e8400-e29b-41d4-a716-446655440000
    ///
    /// </remarks>
    [HttpGet("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetTenantByIdQuery(id));
        if (result == null)
            return NotFound(new { message = "Tenant non trouvé" });
        return Ok(result);
    }

    /// <summary>
    /// Creates a new tenant.
    /// </summary>
    /// <param name="command">The tenant creation command with name, slug, email, and phone.</param>
    /// <returns>Success message with the newly created tenant ID.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/tenants
    ///     {
    ///       "name": "Main Garage",
    ///       "slug": "main-garage",
    ///       "email": "admin@maingarage.com",
    ///       "phone": "+33123456789"
    ///     }
    ///
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateTenantCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message, tenantId = result.TenantId });
    }

    /// <summary>
    /// Updates an existing tenant's information.
    /// </summary>
    /// <param name="id">The tenant ID to update.</param>
    /// <param name="request">The update request with new name, email, and phone.</param>
    /// <returns>Success message.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /api/tenants/550e8400-e29b-41d4-a716-446655440000
    ///     {
    ///       "name": "Updated Garage Name",
    ///       "email": "newemail@garage.com",
    ///       "phone": "+33987654321"
    ///     }
    ///
    /// </remarks>
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTenantRequest request)
    {
        var command = new UpdateTenantCommand(id, request.Name, request.Email, request.Phone);
        var result = await _mediator.Send(command);
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Deletes a tenant from the platform.
    /// </summary>
    /// <param name="id">The tenant ID to delete.</param>
    /// <returns>Success message.</returns>
    /// <remarks>
    /// Note: A tenant can only be deleted if it has no associated garages.
    ///
    /// Sample request:
    ///
    ///     DELETE /api/tenants/550e8400-e29b-41d4-a716-446655440000
    ///
    /// </remarks>
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteTenantCommand(id));
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }
}

/// <summary>
/// Request model for updating tenant information.
/// </summary>
public record UpdateTenantRequest(
    /// <summary>The tenant's name.</summary>
    string Name,
    /// <summary>The tenant's email address (must be unique).</summary>
    string Email,
    /// <summary>The tenant's phone number.</summary>
    string Phone
);
