using MecaManage.Application.Features.SpareParts.Commands;
using MecaManage.Application.Features.SpareParts.Queries;
using MecaManage.API.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MecaManage.API.Controllers;

[ApiController]
[Route("api/garages/{garageId}/stock")]
[Authorize]
public class SparePartsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SparePartsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>GET /api/garages/{garageId}/stock — list all parts (AdminEntreprise, ChefAtelier, or Mecanicien)</summary>
    [HttpGet]
    [Authorize(Roles = "AdminEntreprise,ChefAtelier,Mecanicien")]
    public async Task<IActionResult> GetStock(
        Guid garageId,
        [FromQuery] string? category = null,
        [FromQuery] string? search = null)
    {
        var parts = await _mediator.Send(new GetGarageSparePartsQuery(garageId, category, search));
        return Ok(parts);
    }

    /// <summary>POST /api/garages/{garageId}/stock — AdminEntreprise only</summary>
    [HttpPost]
    [Authorize(Roles = "AdminEntreprise")]
    public async Task<IActionResult> CreatePart(Guid garageId, [FromBody] CreateSparePartRequest body)
    {
        var userId = User.GetUserId();
        var result = await _mediator.Send(new CreateSparePartCommand(
            garageId, userId,
            body.Code, body.Name, body.Description, body.Category,
            body.UnitPrice, body.StockQuantity, body.ReorderLevel,
            body.Manufacturer, body.PartNumber));

        if (!result.Success) return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message, partId = result.PartId });
    }

    /// <summary>PUT /api/garages/{garageId}/stock/{partId} — AdminEntreprise only</summary>
    [HttpPut("{partId:guid}")]
    [Authorize(Roles = "AdminEntreprise")]
    public async Task<IActionResult> UpdatePart(Guid garageId, Guid partId, [FromBody] UpdateSparePartRequest body)
    {
        var userId = User.GetUserId();
        var result = await _mediator.Send(new UpdateSparePartCommand(
            partId, garageId, userId,
            body.Name, body.Description, body.Category,
            body.UnitPrice, body.ReorderLevel,
            body.Manufacturer, body.PartNumber, body.IsActive));

        if (!result.Success) return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }

    /// <summary>PATCH /api/garages/{garageId}/stock/{partId}/restock — AdminEntreprise only</summary>
    [HttpPatch("{partId:guid}/restock")]
    [Authorize(Roles = "AdminEntreprise")]
    public async Task<IActionResult> Restock(Guid garageId, Guid partId, [FromBody] RestockRequest body)
    {
        var userId = User.GetUserId();
        var result = await _mediator.Send(new RestockSparePartCommand(partId, garageId, userId, body.QuantityToAdd));

        if (!result.Success) return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message, newQuantity = result.NewQuantity });
    }

    /// <summary>DELETE /api/garages/{garageId}/stock/{partId} — AdminEntreprise only</summary>
    [HttpDelete("{partId:guid}")]
    [Authorize(Roles = "AdminEntreprise")]
    public async Task<IActionResult> DeletePart(Guid garageId, Guid partId)
    {
        var userId = User.GetUserId();
        var result = await _mediator.Send(new DeleteSparePartCommand(partId, garageId, userId));

        if (!result.Success) return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }
}

// ── Request body shapes ────────────────────────────────────────────────────
public record CreateSparePartRequest(
    string Code,
    string Name,
    string Description,
    string Category,
    decimal UnitPrice,
    int StockQuantity,
    int ReorderLevel,
    string? Manufacturer,
    string? PartNumber
);

public record UpdateSparePartRequest(
    string Name,
    string Description,
    string Category,
    decimal UnitPrice,
    int ReorderLevel,
    string? Manufacturer,
    string? PartNumber,
    bool IsActive
);

public record RestockRequest(int QuantityToAdd);

