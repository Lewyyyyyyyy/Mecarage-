using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Notifications.Commands;

public record MarkNotificationAsReadCommand(
    Guid NotificationId,
    Guid UserId
) : IRequest<MarkNotificationAsReadResult>;

public record MarkNotificationAsReadResult(bool Success, string Message);

public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand, MarkNotificationAsReadResult>
{
    private readonly IApplicationDbContext _context;

    public MarkNotificationAsReadCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MarkNotificationAsReadResult> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.RecipientId == request.UserId, cancellationToken);

        if (notification == null)
            return new MarkNotificationAsReadResult(false, "Notification non trouvée");

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new MarkNotificationAsReadResult(true, "Notification marquée comme lue");
    }
}

