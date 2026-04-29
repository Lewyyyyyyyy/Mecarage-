using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Admin.Queries;

public record GetAdminKpisQuery : IRequest<AdminKpisResult>;

public record AdminKpisResult(
    int TotalTenants,
    int TotalUsers,
    int TotalGarages,
    int TotalVehicles,
    int TotalInterventions,
    int PendingInterventions,
    int CompletedInterventions,
    int ActiveAdmins,
    int ActiveMechanics,
    int ActiveClients
);

public class GetAdminKpisQueryHandler : IRequestHandler<GetAdminKpisQuery, AdminKpisResult>
{
    private readonly IApplicationDbContext _context;

    public GetAdminKpisQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminKpisResult> Handle(GetAdminKpisQuery request, CancellationToken cancellationToken)
    {
        var totalTenants = await _context.Tenants.CountAsync(cancellationToken);

        var totalUsers = await _context.Users.CountAsync(u => !u.IsDeleted, cancellationToken);

        var totalGarages = await _context.Garages.CountAsync(g => g.IsActive, cancellationToken);

        var totalVehicles = await _context.Vehicles.CountAsync(cancellationToken);

        var totalInterventions = await _context.InterventionRequests.CountAsync(cancellationToken);

        var pendingInterventions = await _context.InterventionRequests
            .CountAsync(i => i.Status.ToString() == "Pending" || i.Status.ToString() == "InProgress", cancellationToken);

        var completedInterventions = await _context.InterventionRequests
            .CountAsync(i => i.Status.ToString() == "Completed", cancellationToken);

        var activeAdmins = await _context.Users
            .CountAsync(u => u.IsActive && !u.IsDeleted && (u.Role.ToString() == "AdminEntreprise" || u.Role.ToString() == "ChefAtelier"), cancellationToken);

        var activeMechanics = await _context.Users
            .CountAsync(u => u.IsActive && !u.IsDeleted && u.Role.ToString() == "Mecanicien", cancellationToken);

        var activeClients = await _context.Users
            .CountAsync(u => u.IsActive && !u.IsDeleted && u.Role.ToString() == "Client", cancellationToken);

        return new AdminKpisResult(
            TotalTenants: totalTenants,
            TotalUsers: totalUsers,
            TotalGarages: totalGarages,
            TotalVehicles: totalVehicles,
            TotalInterventions: totalInterventions,
            PendingInterventions: pendingInterventions,
            CompletedInterventions: completedInterventions,
            ActiveAdmins: activeAdmins,
            ActiveMechanics: activeMechanics,
            ActiveClients: activeClients
        );
    }
}

