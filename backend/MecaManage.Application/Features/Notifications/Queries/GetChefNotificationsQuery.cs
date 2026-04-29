using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Notifications.Queries;

public record GetChefNotificationsQuery(Guid ChefId) : IRequest<List<NotificationDto>>;

public record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    string NotificationType,
    bool IsRead,
    DateTime CreatedAt,
    Guid? SymptomReportId,
    string? ClientName,
    string? VehicleInfo
);

public class GetChefNotificationsQueryHandler : IRequestHandler<GetChefNotificationsQuery, List<NotificationDto>>
{
    private readonly IApplicationDbContext _context;

    public GetChefNotificationsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<NotificationDto>> Handle(GetChefNotificationsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .Where(n => n.RecipientId == request.ChefId)
            .Include(n => n.SymptomReport)
                .ThenInclude(sr => sr!.Client)
            .Include(n => n.SymptomReport)
                .ThenInclude(sr => sr!.Vehicle)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto(
                n.Id,
                n.Title,
                n.Message,
                n.NotificationType,
                n.IsRead,
                n.CreatedAt,
                n.SymptomReportId,
                n.SymptomReport != null ? $"{n.SymptomReport.Client.FirstName} {n.SymptomReport.Client.LastName}" : null,
                n.SymptomReport != null ? $"{n.SymptomReport.Vehicle.Brand} {n.SymptomReport.Vehicle.Model} ({n.SymptomReport.Vehicle.Year})" : null
            ))
            .ToListAsync(cancellationToken);
    }
}

