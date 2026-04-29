using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.SymptomReports.Queries;

public record GetClientSymptomReportsQuery(
    Guid ClientId
) : IRequest<List<SymptomReportDto>>;

public record SymptomReportDto(
    Guid Id,
    Guid ClientId,
    Guid VehicleId,
    string VehicleBrand,
    string VehicleModel,
    string SymptomsDescription,
    string? AIPredictedIssue,
    float? AIConfidenceScore,
    string? AIRecommendations,
    string? ChefFeedback,
    string Status,
    DateTime SubmittedAt,
    DateTime? ReviewedAt,
    DateTime? AvailablePeriodStart,
    DateTime? AvailablePeriodEnd,
    Guid? GarageId
);

public class GetClientSymptomReportsQueryHandler : IRequestHandler<GetClientSymptomReportsQuery, List<SymptomReportDto>>
{
    private readonly IApplicationDbContext _context;

    public GetClientSymptomReportsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SymptomReportDto>> Handle(GetClientSymptomReportsQuery request, CancellationToken cancellationToken)
    {
        return await _context.SymptomReports
            .Where(r => r.ClientId == request.ClientId)
            .Include(r => r.Vehicle)
            .OrderByDescending(r => r.SubmittedAt)
            .Select(r => new SymptomReportDto(
                r.Id,
                r.ClientId,
                r.VehicleId,
                r.Vehicle.Brand,
                r.Vehicle.Model,
                r.SymptomsDescription,
                r.AIPredictedIssue,
                r.AIConfidenceScore,
                r.AIRecommendations,
                r.ChefFeedback,
                r.Status.ToString(),
                r.SubmittedAt,
                r.ReviewedAt,
                r.AvailablePeriodStart,
                r.AvailablePeriodEnd,
                r.GarageId
            ))
            .ToListAsync(cancellationToken);
    }
}
