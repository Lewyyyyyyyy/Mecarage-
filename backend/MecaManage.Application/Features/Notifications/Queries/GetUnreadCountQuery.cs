using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Notifications.Queries;

public record GetUnreadCountQuery(Guid UserId) : IRequest<int>;

public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, int>
{
    private readonly IApplicationDbContext _context;

    public GetUnreadCountQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        // Determine user role and garage
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null) return 0;

        // --- ChefAtelier / AdminEntreprise: count pending inbox items by business state ---
        if (user.Role == UserRole.ChefAtelier || user.Role == UserRole.AdminEntreprise)
        {
            if (user.GarageId == null) return 0;

            var garageId = user.GarageId.Value;

            var pendingReports = await _context.SymptomReports
                .CountAsync(r => r.GarageId == garageId
                    && (r.Status == SymptomReportStatus.Submitted || r.Status == SymptomReportStatus.PendingReview),
                    cancellationToken);

            var pendingAppointments = await _context.Appointments
                .CountAsync(a => a.GarageId == garageId && a.Status == AppointmentStatus.Pending,
                    cancellationToken);

            var pendingExaminations = await _context.RepairTaskAssignments
                .CountAsync(a => a.ExaminationStatus == "Pending"
                    && a.RepairTask.GarageId == garageId,
                    cancellationToken);

            return pendingReports + pendingAppointments + pendingExaminations;
        }

        // --- Mecanicien: count newly assigned tasks not yet started ---
        if (user.Role == UserRole.Mecanicien)
        {
            return await _context.RepairTaskAssignments
                .CountAsync(a => a.MechanicId == request.UserId
                    && a.RepairTask.Status == RepairTaskStatus.Assigned,
                    cancellationToken);
        }

        return 0;
    }
}

