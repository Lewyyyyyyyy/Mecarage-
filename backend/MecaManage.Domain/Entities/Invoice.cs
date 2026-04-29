using MecaManage.Domain.Common;
using MecaManage.Domain.Enums;

namespace MecaManage.Domain.Entities;

/// <summary>
/// Represents an invoice (Facture) for repair work and parts.
/// References spare parts from inventory and includes service fees.
/// Client must approve before work begins.
/// </summary>
public class Invoice : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid GarageId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;  // Format: INV-2024-001

    public decimal ServiceFee { get; set; }                      // Labor cost
    public decimal PartsTotal { get; set; }                      // Sum of all parts
    public decimal TotalAmount { get; set; }                     // ServiceFee + PartsTotal + tax
    public decimal? TaxAmount { get; set; }

    public bool ClientApproved { get; set; } = false;
    public DateTime? ClientApprovedAt { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public DateTime? FinalizedAt { get; set; }

    // Navigation properties
    public Appointment Appointment { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
    public User Client { get; set; } = null!;
    public Garage Garage { get; set; } = null!;
    public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
}

/// <summary>
/// Represents a line item in an invoice (spare part or service).
/// </summary>
public class InvoiceLineItem : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public Guid? SparePartId { get; set; }              // Reference to spare part from inventory

    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }              // Quantity * UnitPrice

    // Navigation properties
    public Invoice Invoice { get; set; } = null!;
    public SparePart? SparePart { get; set; }
}

