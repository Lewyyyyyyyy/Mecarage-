using MecaManage.Domain.Common;

namespace MecaManage.Domain.Entities;

public class AIDiagnosis : BaseEntity
{
    public Guid InterventionRequestId { get; set; }
    public string Diagnosis { get; set; } = string.Empty;
    public float ConfidenceScore { get; set; }
    public string RecommendedWorkshop { get; set; } = string.Empty;
    public string UrgencyLevel { get; set; } = string.Empty;
    public string EstimatedCostRange { get; set; } = string.Empty;
    public string RecommendedActions { get; set; } = string.Empty;
    public int RagSourcesUsed { get; set; }

    public InterventionRequest InterventionRequest { get; set; } = null!;
}