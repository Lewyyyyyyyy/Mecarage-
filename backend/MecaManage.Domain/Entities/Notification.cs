using MecaManage.Domain.Common;

namespace MecaManage.Domain.Entities;

/// <summary>
/// Represents a notification sent to a user (typically a chef d'atelier)
/// about important events like new symptom reports.
/// </summary>
public class Notification : BaseEntity
{
    public Guid RecipientId { get; set; }      // The user who should receive the notification
    public Guid? SymptomReportId { get; set; }  // Reference to the symptom report if applicable
    public Guid? AppointmentId { get; set; }    // Reference to appointment if applicable
    public Guid? RepairTaskId { get; set; }     // Reference to repair task if applicable
    public Guid? InvoiceId { get; set; }        // Reference to invoice if applicable

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string NotificationType { get; set; } = "SymptomReportSubmitted"; // Type for filtering

    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }

    // Navigation properties
    public User Recipient { get; set; } = null!;
    public SymptomReport? SymptomReport { get; set; }
}

