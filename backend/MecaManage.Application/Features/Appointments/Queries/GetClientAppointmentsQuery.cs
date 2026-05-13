using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Appointments.Queries;

public record GetClientAppointmentsQuery(
    Guid ClientId
) : IRequest<List<ClientAppointmentDto>>;

public record ClientAppointmentDto(
    Guid Id,
    Guid VehicleId,
    Guid? SymptomReportId,
    string VehicleInfo,
    string GarageName,
    DateTime PreferredDate,
    TimeSpan PreferredTime,
    string Status,
    DateTime CreatedAt
);

public class GetClientAppointmentsQueryHandler : IRequestHandler<GetClientAppointmentsQuery, List<ClientAppointmentDto>>
{
    private readonly IApplicationDbContext _context;

    public GetClientAppointmentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ClientAppointmentDto>> Handle(GetClientAppointmentsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Appointments
            .Where(a => a.ClientId == request.ClientId)
            .Include(a => a.Vehicle)
            .Include(a => a.Garage)
            .OrderByDescending(a => a.PreferredDate)
            .Select(a => new ClientAppointmentDto(
                a.Id,
                a.VehicleId,
                a.SymptomReportId,
                $"{a.Vehicle.Brand} {a.Vehicle.Model} ({a.Vehicle.Year})",
                a.Garage.Name,
                a.PreferredDate,
                a.PreferredTime,
                a.Status.ToString(),
                a.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }
}

