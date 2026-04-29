using MecaManage.Domain.Common;
using MecaManage.Domain.Enums;

namespace MecaManage.Domain.Entities;

/// <summary>
/// Represents a service appointment booking at a garage.
/// Linked to a vehicle, garage, and optionally a symptom report.
/// Chef must approve before work begins.
/// </summary>
public class Appointment : BaseEntity
{
    public Guid ClientId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid GarageId { get; set; }
    public Guid? SymptomReportId { get; set; }     // Optional: linked to a diagnostic report

    public DateTime PreferredDate { get; set; }
    public TimeSpan PreferredTime { get; set; }
    public string? SpecialRequests { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

    // Chef approval
    public Guid? ApprovedByChefId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? DeclineReason { get; set; }


    // Navigation properties
    public User Client { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
    public Garage Garage { get; set; } = null!;
    public SymptomReport? SymptomReport { get; set; }
    public User? ApprovedByChef { get; set; }

    public Invoice? Invoice { get; set; }
    public RepairTask? RepairTask { get; set; }
}

