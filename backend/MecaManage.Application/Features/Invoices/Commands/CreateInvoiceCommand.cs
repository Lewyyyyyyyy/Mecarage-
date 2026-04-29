using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Entities;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Invoices.Commands;

public record CreateInvoiceCommand(
    Guid AppointmentId,
    decimal ServiceFee,
    List<CreateInvoiceLineItemDto>? LineItems = null
) : IRequest<CreateInvoiceResult>;

public record CreateInvoiceLineItemDto(
    Guid? SparePartId,
    string Description,
    int Quantity,
    decimal UnitPrice
);

public record CreateInvoiceResult(bool Success, string Message, Guid? InvoiceId);

public class CreateInvoiceCommandHandler : IRequestHandler<CreateInvoiceCommand, CreateInvoiceResult>
{
    private readonly IApplicationDbContext _context;

    public CreateInvoiceCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CreateInvoiceResult> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Client)
            .Include(a => a.Garage)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);

        if (appointment == null)
            return new CreateInvoiceResult(false, "Rendez-vous introuvable", null);

        if (appointment.Status != AppointmentStatus.Approved)
            return new CreateInvoiceResult(false, "Seuls les rendez-vous approuvés peuvent avoir une facture", null);

        var invoice = new Invoice
        {
            AppointmentId = request.AppointmentId,
            TenantId = appointment.Client.TenantId!.Value,
            ClientId = appointment.ClientId,
            GarageId = appointment.GarageId,
            InvoiceNumber = GenerateInvoiceNumber(),
            ServiceFee = request.ServiceFee,
            Status = InvoiceStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        var partsTotal = 0m;

        if (request.LineItems != null && request.LineItems.Count > 0)
        {
            foreach (var item in request.LineItems)
            {
                var lineTotal = item.Quantity * item.UnitPrice;
                partsTotal += lineTotal;

                var lineItem = new InvoiceLineItem
                {
                    SparePartId = item.SparePartId,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    LineTotal = lineTotal
                };

                invoice.LineItems.Add(lineItem);
            }
        }

        invoice.PartsTotal = partsTotal;
        invoice.TotalAmount = invoice.ServiceFee + partsTotal;

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateInvoiceResult(true, "Facture créée avec succès", invoice.Id);
    }

    private string GenerateInvoiceNumber()
    {
        // Format: INV-YYYY-MM-XXXXX
        return $"INV-{DateTime.UtcNow:yyyy-MM}-{Guid.NewGuid().ToString().Substring(0, 5).ToUpper()}";
    }
}

