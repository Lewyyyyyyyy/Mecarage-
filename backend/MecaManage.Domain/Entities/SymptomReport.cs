using MecaManage.Domain.Common;
using MecaManage.Domain.Enums;

namespace MecaManage.Domain.Entities;

/// <summary>
/// Represents a symptom report submitted by a client about their vehicle.
/// The AI diagnoses the issue, and the Chef reviews and adds professional feedback.
/// </summary>
public class SymptomReport : BaseEntity
{
    public Guid ClientId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid? GarageId { get; set; }  // Assigned garage after Chef reviews

    public string SymptomsDescription { get; set; } = string.Empty;

    // AI Diagnosis response (stored as JSON)
    public string? AIPredictedIssue { get; set; }
    public float? AIConfidenceScore { get; set; }
    public string? AIRecommendations { get; set; }

    // Chef's professional review
    public string? ChefFeedback { get; set; }
    public Guid? ReviewedByChefId { get; set; }      // The Chef who reviewed this
    public DateTime? ReviewedAt { get; set; }

    // Chef sets the available period for the client to book a rendez-vous
    public DateTime? AvailablePeriodStart { get; set; }
    public DateTime? AvailablePeriodEnd { get; set; }

    public SymptomReportStatus Status { get; set; } = SymptomReportStatus.Submitted;
    public DateTime SubmittedAt { get; set; }

    // Navigation properties
    public User Client { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
    public Garage? Garage { get; set; }
    public User? ReviewedByChef { get; set; }
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}

