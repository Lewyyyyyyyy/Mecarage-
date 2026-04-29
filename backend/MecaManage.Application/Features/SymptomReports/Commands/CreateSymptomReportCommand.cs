using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Entities;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.SymptomReports.Commands;

public record CreateSymptomReportCommand(
    Guid ClientId,
    Guid VehicleId,
    string SymptomsDescription,
    Guid? GarageId = null,
    Guid? ChefAtelierId = null
) : IRequest<CreateSymptomReportResult>;

public record CreateSymptomReportResult(bool Success, string Message, Guid? ReportId);

public class CreateSymptomReportCommandHandler : IRequestHandler<CreateSymptomReportCommand, CreateSymptomReportResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IIAService _iaService;

    public CreateSymptomReportCommandHandler(IApplicationDbContext context, IIAService iaService)
    {
        _context = context;
        _iaService = iaService;
    }

    public async Task<CreateSymptomReportResult> Handle(CreateSymptomReportCommand request, CancellationToken cancellationToken)
    {
        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId && v.ClientId == request.ClientId, cancellationToken);

        if (vehicle == null)
            return new CreateSymptomReportResult(false, "Véhicule introuvable ou vous n'avez pas accès à ce véhicule", null);

        // Make garageId required
        if (request.GarageId == null || request.GarageId == Guid.Empty)
            return new CreateSymptomReportResult(false, "Un garage doit être sélectionné pour soumettre un rapport de symptômes", null);

        // Verify garage exists
        var garage = await _context.Garages
            .FirstOrDefaultAsync(g => g.Id == request.GarageId, cancellationToken);

        if (garage == null)
            return new CreateSymptomReportResult(false, "Le garage sélectionné n'existe pas", null);

        var diagnosis = await _iaService.GetDiagnosisAsync(
            request.SymptomsDescription,
            vehicle.Brand,
            vehicle.Model,
            vehicle.Year,
            vehicle.Mileage,
            vehicle.FuelType,
            cancellationToken,
            request.ChefAtelierId,
            request.GarageId);


        var report = new SymptomReport
        {
            ClientId = request.ClientId,
            VehicleId = request.VehicleId,
            GarageId = request.GarageId,
            SymptomsDescription = request.SymptomsDescription,
            AIPredictedIssue = diagnosis?.Diagnosis,
            AIConfidenceScore = diagnosis?.ConfidenceScore,
            AIRecommendations = diagnosis?.RecommendedActions,
            Status = diagnosis == null ? SymptomReportStatus.Submitted : SymptomReportStatus.PendingReview,
            SubmittedAt = DateTime.UtcNow
        };

        _context.SymptomReports.Add(report);
        await _context.SaveChangesAsync(cancellationToken);

        // Send notification to chef
        await SendNotificationToChef(request.GarageId.Value, report.Id, vehicle, cancellationToken);

        return new CreateSymptomReportResult(
            true,
            diagnosis == null
                ? "Rapport de symptômes créé avec succès"
                : "Rapport de symptômes créé avec diagnostic IA",
            report.Id);
    }

    private async Task SendNotificationToChef(Guid garageId, Guid reportId, Vehicle vehicle, CancellationToken cancellationToken)
    {
        try
        {
            var garage = await _context.Garages
                .FirstOrDefaultAsync(g => g.Id == garageId, cancellationToken);

            if (garage == null) return;

            // Collect recipient IDs: garage AdminId + any ChefAtelier staff with User.GarageId == garageId
            var recipientIds = new HashSet<Guid>();

            if (garage.AdminId.HasValue)
                recipientIds.Add(garage.AdminId.Value);

            var chefStaffIds = await _context.Users
                .Where(u => u.GarageId == garageId && u.Role == UserRole.ChefAtelier && u.IsActive)
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            foreach (var id in chefStaffIds)
                recipientIds.Add(id);

            foreach (var recipientId in recipientIds)
            {
                var notification = new Notification
                {
                    RecipientId = recipientId,
                    SymptomReportId = reportId,
                    Title = "Nouveau Rapport de Symptômes",
                    Message = $"Nouveau rapport pour {vehicle.Brand} {vehicle.Model} ({vehicle.Year})",
                    NotificationType = "SymptomReportSubmitted",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                _context.Notifications.Add(notification);
            }

            if (recipientIds.Count > 0)
                await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending notification to chef: {ex.Message}");
        }
    }
}


