using MecaManage.Domain.Common;
using MecaManage.Domain.Enums;

namespace MecaManage.Domain.Entities;

public class InterventionRequest : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid GarageId { get; set; }
    public Guid? AssignedMecanicienId { get; set; }
    public string Description { get; set; } = string.Empty;
    public InterventionStatus Status { get; set; } = InterventionStatus.EnAttente;
    public UrgencyLevel UrgencyLevel { get; set; } = UrgencyLevel.Modere;
    public DateTime? AppointmentDate { get; set; }
    public string? DiagnosisResult { get; set; }
    public string? Notes { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public User Client { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
    public Garage Garage { get; set; } = null!;
    public User? AssignedMecanicien { get; set; }
    public AIDiagnosis? AIDiagnosis { get; set; }
}