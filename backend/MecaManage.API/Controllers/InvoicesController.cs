using MecaManage.Application.Features.Invoices.Commands;
using MecaManage.Application.Features.Invoices.Queries;
using MecaManage.API.Extensions;
using MecaManage.API.Pdf;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MecaManage.API.Controllers;

/// <summary>
/// Manages invoices (Factures) for repair services.
/// Chef d'Atelier creates invoices with spare parts and service fees.
/// Clients approve invoices to proceed with work.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public InvoicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new invoice for an approved appointment.
    /// </summary>
    /// <param name="command">The invoice creation command.</param>
    /// <returns>Success message with the newly created invoice ID.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/invoices
    ///     {
    ///       "appointmentId": "550e8400-e29b-41d4-a716-446655440000",
    ///       "serviceFee": 150.00,
    ///       "lineItems": [
    ///         {
    ///           "description": "Oil filter replacement",
    ///           "quantity": 1,
    ///           "unitPrice": 25.50
    ///         },
    ///         {
    ///           "sparePartId": "660e8400-e29b-41d4-a716-446655440000",
    ///           "description": "Engine oil 5L",
    ///           "quantity": 2,
    ///           "unitPrice": 45.00
    ///         }
    ///       ]
    ///     }
    ///
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = "ChefAtelier")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message, invoiceId = result.InvoiceId });
    }

    /// <summary>
    /// Gets all invoices for the authenticated client.
    /// </summary>
    /// <returns>List of client's invoices.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/invoices/my-invoices
    ///
    /// </remarks>
    [HttpGet("my-invoices")]
    [Authorize(Roles = "Client")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyInvoices()
    {
        var clientId = User.GetUserId();
        var result = await _mediator.Send(new GetClientInvoicesQuery(clientId));
        return Ok(result);
    }

    /// <summary>
    /// Gets all invoices for a garage where the chef works.
    /// </summary>
    /// <param name="garageId">The garage ID.</param>
    /// <returns>List of invoices for the garage.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/invoices/garage/880e8400-e29b-41d4-a716-446655440000
    ///
    /// </remarks>
    [HttpGet("garage/{garageId}")]
    [Authorize(Roles = "ChefAtelier,AdminEntreprise")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetGarageInvoices(Guid garageId)
    {
        var chefId = User.GetUserId();
        var result = await _mediator.Send(new GetGarageInvoicesQuery(garageId, chefId));
        return Ok(result);
    }

    /// <summary>
    /// Finalizes a draft invoice and sends it to the client for approval.
    /// </summary>
    /// <param name="invoiceId">The invoice ID to finalize.</param>
    /// <returns>Success message.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     PATCH /api/invoices/550e8400-e29b-41d4-a716-446655440000/finalize
    ///
    /// </remarks>
    [HttpPatch("{invoiceId}/finalize")]
    [Authorize(Roles = "ChefAtelier")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> FinalizeInvoice(Guid invoiceId)
    {
        var chefId = User.GetUserId();
        var result = await _mediator.Send(new FinalizeInvoiceCommand(invoiceId, chefId));
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Approves an invoice and allows work to proceed.
    /// </summary>
    /// <param name="invoiceId">The invoice ID to approve.</param>
    /// <returns>Success message.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     PATCH /api/invoices/550e8400-e29b-41d4-a716-446655440000/approve
    ///
    /// </remarks>
    [HttpPatch("{invoiceId}/approve")]
    [Authorize(Roles = "Client")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ApproveInvoice(Guid invoiceId)
    {
        var clientId = User.GetUserId();
        var result = await _mediator.Send(new ApproveInvoiceCommand(invoiceId, clientId));
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }

    [HttpPatch("{invoiceId}/reject")]
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> RejectInvoice(Guid invoiceId)
    {
        var clientId = User.GetUserId();
        var result = await _mediator.Send(new RejectInvoiceCommand(invoiceId, clientId));
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Downloads a PDF version of an invoice.
    /// </summary>
    [HttpGet("{invoiceId}/pdf")]
    [Authorize(Roles = "Client,ChefAtelier,AdminEntreprise")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadInvoicePdf(Guid invoiceId)
    {
        var userId = User.GetUserId();
        var dto = await _mediator.Send(new GetInvoicePdfQuery(invoiceId, userId));

        if (dto is null)
            return NotFound(new { message = "Facture introuvable ou accès non autorisé." });

        var doc = new InvoicePdfDocument(dto);
        var pdfBytes = doc.GeneratePdf();

        return File(pdfBytes, "application/pdf", $"Facture-{dto.InvoiceNumber}.pdf");
    }
}

