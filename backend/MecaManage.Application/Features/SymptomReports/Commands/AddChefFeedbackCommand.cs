using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Entities;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.SymptomReports.Commands;

public record AddChefFeedbackCommand(
    Guid ReportId,
    Guid ChefId,
    string Feedback,
    SymptomReportStatus NewStatus,
    DateTime? AvailablePeriodStart = null,
    DateTime? AvailablePeriodEnd = null
) : IRequest<AddChefFeedbackResult>;

public record AddChefFeedbackResult(bool Success, string Message);

public class AddChefFeedbackCommandHandler : IRequestHandler<AddChefFeedbackCommand, AddChefFeedbackResult>
{
    private readonly IApplicationDbContext _context;

    public AddChefFeedbackCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AddChefFeedbackResult> Handle(AddChefFeedbackCommand request, CancellationToken cancellationToken)
    {
        var report = await _context.SymptomReports
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken);

        if (report == null)
            return new AddChefFeedbackResult(false, "Rapport non trouvé");

        var chef = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.ChefId && (u.Role == UserRole.ChefAtelier || u.Role == UserRole.AdminEntreprise), cancellationToken);

        if (chef == null)
            return new AddChefFeedbackResult(false, "Chef d'atelier non trouvé");

        if (request.AvailablePeriodStart.HasValue && request.AvailablePeriodEnd.HasValue
            && request.AvailablePeriodStart.Value > request.AvailablePeriodEnd.Value)
        {
            return new AddChefFeedbackResult(false, "La date de début ne peut pas être supérieure à la date de fin.");
        }

        report.ChefFeedback = request.Feedback;
        report.ReviewedByChefId = request.ChefId;
        report.ReviewedAt = DateTime.UtcNow;
        report.Status = request.NewStatus;
        report.AvailablePeriodStart = request.AvailablePeriodStart;
        report.AvailablePeriodEnd = request.AvailablePeriodEnd;

        _context.SymptomReports.Update(report);

        // Notify the client about the chef's feedback
        var notificationType = request.NewStatus == SymptomReportStatus.Approved
            ? "ChefFeedbackApproved"
            : "ChefFeedbackDeclined";

        var notificationTitle = request.NewStatus == SymptomReportStatus.Approved
            ? "Rapport approuvé - Prenez rendez-vous"
            : "Rapport examiné par le chef d'atelier";

        var notificationMessage = request.NewStatus == SymptomReportStatus.Approved
            ? $"Le chef d'atelier a approuvé votre rapport. Vous pouvez maintenant choisir un rendez-vous" +
              (request.AvailablePeriodStart.HasValue && request.AvailablePeriodEnd.HasValue
                  ? $" entre le {request.AvailablePeriodStart.Value:dd/MM/yyyy} et le {request.AvailablePeriodEnd.Value:dd/MM/yyyy}."
                  : ".")
            : "Le chef d'atelier a examiné votre rapport et vous a laissé un retour.";

        var clientNotification = new Notification
        {
            RecipientId = report.ClientId,
            SymptomReportId = report.Id,
            Title = notificationTitle,
            Message = notificationMessage,
            NotificationType = notificationType,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.Notifications.Add(clientNotification);
        await _context.SaveChangesAsync(cancellationToken);

        return new AddChefFeedbackResult(true, "Feedback du chef ajouté avec succès");
    }
}
