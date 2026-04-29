using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MecaManage.Application.Common.Interfaces;
using MecaManage.Application.Common.Models;

namespace MecaManage.Infrastructure.Services;

public class IAService : IIAService
{
    private readonly HttpClient _httpClient;

    public IAService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IADiagnosisResponse?> GetDiagnosisAsync(
        string description,
        string brand,
        string model,
        int year,
        int mileage,
        string fuelType,
        CancellationToken cancellationToken = default,
        Guid? chefAtelierId = null,
        Guid? garageId = null)
    {
        var request = new
        {
            symptoms = description,
            vehicle_brand = brand,
            vehicle_model = model,
            vehicle_year = year,
            mileage = mileage,
            fuel_type = fuelType,
            chef_atelier_id = chefAtelierId,
            garage_id = garageId
        };

        var response = await _httpClient.PostAsJsonAsync("/diagnose", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        var apiResponse = await response.Content.ReadFromJsonAsync<DiagnosisApiResponse>(cancellationToken: cancellationToken);
        if (apiResponse == null)
            return null;

        return new IADiagnosisResponse
        {
            Diagnosis = apiResponse.Diagnosis,
            ConfidenceScore = apiResponse.ConfidenceScore,
            RecommendedWorkshop = apiResponse.RecommendedWorkshop,
            UrgencyLevel = apiResponse.UrgencyLevel,
            EstimatedCostRange = apiResponse.EstimatedCostRange,
            RecommendedActions = string.Join(" | ", apiResponse.RecommendedActions ?? []),
            RagSourcesUsed = apiResponse.RagSourcesUsed
        };
    }

    private sealed class DiagnosisApiResponse
    {
        [JsonPropertyName("diagnosis")]
        public string Diagnosis { get; set; } = string.Empty;

        [JsonPropertyName("confidence_score")]
        public float ConfidenceScore { get; set; }

        [JsonPropertyName("recommended_workshop")]
        public string RecommendedWorkshop { get; set; } = string.Empty;

        [JsonPropertyName("urgency_level")]
        public string UrgencyLevel { get; set; } = string.Empty;

        [JsonPropertyName("estimated_cost_range")]
        public string EstimatedCostRange { get; set; } = string.Empty;

        [JsonPropertyName("recommended_actions")]
        public List<string>? RecommendedActions { get; set; }

        [JsonPropertyName("rag_sources_used")]
        public int RagSourcesUsed { get; set; }
    }
}