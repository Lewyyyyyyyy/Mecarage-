using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Notifications.Queries;

public record GetMyNotificationsQuery(Guid UserId) : IRequest<List<MyNotificationDto>>;

public record MyNotificationDto(
    Guid Id,
    string Title,
    string Message,
    string NotificationType,
    bool IsRead,
    DateTime CreatedAt
);

public class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, List<MyNotificationDto>>
{
    private readonly IApplicationDbContext _context;

    public GetMyNotificationsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<MyNotificationDto>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .Where(n => n.RecipientId == request.UserId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new MyNotificationDto(
                n.Id,
                n.Title,
                n.Message,
                n.NotificationType,
                n.IsRead,
                n.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }
}

