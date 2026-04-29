using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Appointments.Queries;

public record GetChefPendingAppointmentsQuery(
    Guid ChefId,
    Guid GarageId
) : IRequest<List<PendingAppointmentDto>>;

public record PendingAppointmentDto(
    Guid Id,
    Guid ClientId,
    string ClientName,
    Guid VehicleId,
    string VehicleInfo,
    DateTime PreferredDate,
    TimeSpan PreferredTime,
    string? SymptomSummary,
    DateTime CreatedAt
);

public class GetChefPendingAppointmentsQueryHandler : IRequestHandler<GetChefPendingAppointmentsQuery, List<PendingAppointmentDto>>
{
    private readonly IApplicationDbContext _context;

    public GetChefPendingAppointmentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PendingAppointmentDto>> Handle(GetChefPendingAppointmentsQuery request, CancellationToken cancellationToken)
    {
        // Accept ChefAtelier OR AdminEntreprise belonging to this garage
        var belongs = await _context.Users
            .AnyAsync(u => u.Id == request.ChefId
                && (u.Role == UserRole.ChefAtelier || u.Role == UserRole.AdminEntreprise)
                && u.GarageId == request.GarageId, cancellationToken);

        if (!belongs)
        {
            // Also accept if user is the garage's AdminId
            belongs = await _context.Garages
                .AnyAsync(g => g.Id == request.GarageId && g.AdminId == request.ChefId, cancellationToken);
        }

        if (!belongs)
            return new List<PendingAppointmentDto>();

        var appointments = await _context.Appointments
            .Where(a => a.GarageId == request.GarageId && a.Status == AppointmentStatus.Pending)
            .OrderBy(a => a.PreferredDate)
            .ToListAsync(cancellationToken);

        if (!appointments.Any())
            return new List<PendingAppointmentDto>();

        var clientIds = appointments.Select(a => a.ClientId).Distinct().ToList();
        var vehicleIds = appointments.Select(a => a.VehicleId).Distinct().ToList();
        var reportIds = appointments.Where(a => a.SymptomReportId.HasValue)
            .Select(a => a.SymptomReportId!.Value).Distinct().ToList();

        var clients = await _context.Users.IgnoreQueryFilters()
            .Where(u => clientIds.Contains(u.Id)).ToListAsync(cancellationToken);
        var vehicles = await _context.Vehicles.IgnoreQueryFilters()
            .Where(v => vehicleIds.Contains(v.Id)).ToListAsync(cancellationToken);
        var reports = await _context.SymptomReports
            .Where(r => reportIds.Contains(r.Id))
            .Select(r => new { r.Id, r.SymptomsDescription })
            .ToListAsync(cancellationToken);

        var clientMap = clients.ToDictionary(c => c.Id);
        var vehicleMap = vehicles.ToDictionary(v => v.Id);
        var reportMap = reports.ToDictionary(r => r.Id, r => r.SymptomsDescription);

        return appointments.Select(a =>
        {
            clientMap.TryGetValue(a.ClientId, out var client);
            vehicleMap.TryGetValue(a.VehicleId, out var vehicle);
            var symptomSummary = a.SymptomReportId.HasValue && reportMap.TryGetValue(a.SymptomReportId.Value, out var s) ? s : null;
            return new PendingAppointmentDto(
                a.Id,
                a.ClientId,
                client != null ? $"{client.FirstName} {client.LastName}" : "Client inconnu",
                a.VehicleId,
                vehicle != null ? $"{vehicle.Brand} {vehicle.Model} ({vehicle.Year})" : "Véhicule inconnu",
                a.PreferredDate,
                a.PreferredTime,
                symptomSummary,
                a.CreatedAt
            );
        }).ToList();
    }
}

