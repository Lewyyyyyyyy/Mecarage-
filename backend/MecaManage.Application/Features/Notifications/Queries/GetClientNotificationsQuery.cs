using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Notifications.Queries;

public record GetClientNotificationsQuery(Guid ClientId) : IRequest<List<ClientNotificationDto>>;

public record ClientNotificationDto(
    Guid Id,
    string Title,
    string Message,
    string NotificationType,
    bool IsRead,
    DateTime CreatedAt,
    Guid? SymptomReportId,
    Guid? AppointmentId,
    Guid? RepairTaskId,
    Guid? InvoiceId
);

public class GetClientNotificationsQueryHandler : IRequestHandler<GetClientNotificationsQuery, List<ClientNotificationDto>>
{
    private readonly IApplicationDbContext _context;

    public GetClientNotificationsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ClientNotificationDto>> Handle(GetClientNotificationsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .Where(n => n.RecipientId == request.ClientId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new ClientNotificationDto(
                n.Id,
                n.Title,
                n.Message,
                n.NotificationType,
                n.IsRead,
                n.CreatedAt,
                n.SymptomReportId,
                n.AppointmentId,
                n.RepairTaskId,
                n.InvoiceId
            ))
            .ToListAsync(cancellationToken);
    }
}

