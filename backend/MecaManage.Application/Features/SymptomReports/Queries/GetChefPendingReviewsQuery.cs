using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.SymptomReports.Queries;

public record GetChefPendingReviewsQuery(
    Guid ChefId,
    Guid GarageId
) : IRequest<List<PendingReviewDto>>;

public record PendingReviewDto(
    Guid Id,
    Guid ClientId,
    string ClientName,
    Guid VehicleId,
    string VehicleInfo,
    string SymptomsDescription,
    string? AIPredictedIssue,
    float? AIConfidenceScore,
    string? AIRecommendations,
    DateTime SubmittedAt,
    string Status,
    string? ChefFeedback,
    DateTime? ReviewedAt,
    DateTime? AvailablePeriodStart,
    DateTime? AvailablePeriodEnd
);

public class GetChefPendingReviewsQueryHandler : IRequestHandler<GetChefPendingReviewsQuery, List<PendingReviewDto>>
{
    private readonly IApplicationDbContext _context;

    public GetChefPendingReviewsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PendingReviewDto>> Handle(GetChefPendingReviewsQuery request, CancellationToken cancellationToken)
    {
        // Verify the requesting user is a valid chef/admin for this garage
        var isValidUser = await _context.Users
            .Where(u => u.Id == request.ChefId &&
                        (u.Role == UserRole.ChefAtelier || u.Role == UserRole.AdminEntreprise))
            .AnyAsync(cancellationToken);

        if (!isValidUser)
            return new List<PendingReviewDto>();

        // Check that user belongs to this garage via GarageId or is the garage AdminId
        var belongs = await _context.Users
            .Where(u => u.Id == request.ChefId && u.GarageId == request.GarageId)
            .AnyAsync(cancellationToken);

        if (!belongs)
        {
            belongs = await _context.Garages
                .Where(g => g.Id == request.GarageId && g.AdminId == request.ChefId)
                .AnyAsync(cancellationToken);
        }

        if (!belongs)
            return new List<PendingReviewDto>();

        // Fetch ALL non-archived reports for this garage (pending + already reviewed)
        var reports = await _context.SymptomReports
            .Where(r => r.Status != SymptomReportStatus.Archived
                        && r.GarageId == request.GarageId)
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync(cancellationToken);

        if (!reports.Any())
            return new List<PendingReviewDto>();

        var clientIds = reports.Select(r => r.ClientId).Distinct().ToList();
        var vehicleIds = reports.Select(r => r.VehicleId).Distinct().ToList();

        var clients = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => clientIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        var vehicles = await _context.Vehicles
            .IgnoreQueryFilters()
            .Where(v => vehicleIds.Contains(v.Id))
            .ToListAsync(cancellationToken);

        var clientMap = clients.ToDictionary(c => c.Id);
        var vehicleMap = vehicles.ToDictionary(v => v.Id);

        return reports.Select(r =>
        {
            clientMap.TryGetValue(r.ClientId, out var client);
            vehicleMap.TryGetValue(r.VehicleId, out var vehicle);
            return new PendingReviewDto(
                r.Id,
                r.ClientId,
                client != null ? $"{client.FirstName} {client.LastName}" : "Client inconnu",
                r.VehicleId,
                vehicle != null ? $"{vehicle.Brand} {vehicle.Model} ({vehicle.Year})" : "Véhicule inconnu",
                r.SymptomsDescription,
                r.AIPredictedIssue,
                r.AIConfidenceScore,
                r.AIRecommendations,
                r.SubmittedAt,
                r.Status.ToString(),
                r.ChefFeedback,
                r.ReviewedAt,
                r.AvailablePeriodStart,
                r.AvailablePeriodEnd
            );
        }).ToList();
    }
}
