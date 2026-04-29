using MecaManage.Domain.Common;
using MecaManage.Domain.Enums;

namespace MecaManage.Domain.Entities;

/// <summary>
/// Represents a repair task assigned to mechanics for an appointment.
/// Tracks the workflow from assignment through completion with state machine validation.
/// </summary>
public class RepairTask : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Guid GarageId { get; set; }
    public Guid TenantId { get; set; }
    public Guid AssignedByChefId { get; set; }         // The Chef who created this task

    public string TaskTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public RepairTaskStatus Status { get; set; } = RepairTaskStatus.Assigned;
    public DateTime AssignedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public string? CompletionNotes { get; set; }       // Final notes from mechanic

    // Estimated vs Actual
    public int? EstimatedMinutes { get; set; }         // Estimated time to complete
    public int? ActualMinutes { get; set; }            // Actual time spent

    // Navigation properties
    public Appointment Appointment { get; set; } = null!;
    public Garage Garage { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
    public User AssignedByChef { get; set; } = null!;

    // Many-to-Many: Multiple mechanics can work on one task
    public ICollection<RepairTaskAssignment> Assignments { get; set; } = new List<RepairTaskAssignment>();
}

/// <summary>
/// Join table for Many-to-Many relationship between RepairTask and Mechanic (User).
/// </summary>
public class RepairTaskAssignment : BaseEntity
{
    public Guid RepairTaskId { get; set; }
    public Guid MechanicId { get; set; }

    public DateTime AssignedAt { get; set; }
    public DateTime? StartedWorkAt { get; set; }
    public DateTime? CompletedWorkAt { get; set; }
    public string? MechanicNotes { get; set; }

    // Mechanic examination report (submitted before chef approves work)
    public string? ExaminationObservations { get; set; }
    public string? PartsNeeded { get; set; }          // JSON: [{name, quantity, estimatedPrice}]
    public string? ExaminationFileUrl { get; set; }   // Optional: URL to attached file/photo
    public DateTime? ExaminationSubmittedAt { get; set; }
    public string ExaminationStatus { get; set; } = "None"; // None/Pending/ApprovedByChef/DeclinedByChef

    // Navigation properties
    public RepairTask RepairTask { get; set; } = null!;
    public User Mechanic { get; set; } = null!;
}

