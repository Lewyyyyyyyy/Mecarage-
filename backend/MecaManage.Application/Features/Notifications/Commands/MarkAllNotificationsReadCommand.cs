using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Notifications.Commands;

public record MarkAllNotificationsReadCommand(Guid UserId) : IRequest<int>;

public class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand, int>
{
    private readonly IApplicationDbContext _context;

    public MarkAllNotificationsReadCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var unread = await _context.Notifications
            .Where(n => n.RecipientId == request.UserId && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var n in unread)
            n.IsRead = true;

        await _context.SaveChangesAsync(cancellationToken);
        return unread.Count;
    }
}

