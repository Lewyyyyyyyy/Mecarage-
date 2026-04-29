using MecaManage.Domain.Common;

namespace MecaManage.Domain.Entities;

/// <summary>
/// Represents a spare part in the garage inventory.
/// Each garage has it's own stock of parts.
/// </summary>
public class SparePart : BaseEntity
{
    public Guid GarageId { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;  // Engine, Transmission, Suspension, etc.

    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
    public int ReorderLevel { get; set; }              // Alert when stock falls below this
    public string? Manufacturer { get; set; }
    public string? PartNumber { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime LastRestocked { get; set; }

    // Navigation properties
    public Garage Garage { get; set; } = null!;
    public ICollection<InvoiceLineItem> InvoiceLineItems { get; set; } = new List<InvoiceLineItem>();
}

