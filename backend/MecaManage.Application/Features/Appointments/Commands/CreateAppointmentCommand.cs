using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Entities;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Appointments.Commands;

public record CreateAppointmentCommand(
    Guid ClientId,
    Guid VehicleId,
    Guid GarageId,
    DateTime PreferredDate,
    TimeSpan PreferredTime,
    Guid? SymptomReportId = null,
    string? SpecialRequests = null
) : IRequest<CreateAppointmentResult>;

public record CreateAppointmentResult(bool Success, string Message, Guid? AppointmentId);

public class CreateAppointmentCommandHandler : IRequestHandler<CreateAppointmentCommand, CreateAppointmentResult>
{
    private readonly IApplicationDbContext _context;

    public CreateAppointmentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CreateAppointmentResult> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        // Verify vehicle belongs to client
        var vehicleExists = await _context.Vehicles
            .AnyAsync(v => v.Id == request.VehicleId && v.ClientId == request.ClientId, cancellationToken);

        if (!vehicleExists)
            return new CreateAppointmentResult(false, "Véhicule introuvable ou accès refusé", null);

        // Verify garage exists
        var garageExists = await _context.Garages
            .AnyAsync(g => g.Id == request.GarageId, cancellationToken);

        if (!garageExists)
            return new CreateAppointmentResult(false, "Garage introuvable", null);

        // If symptom report provided, verify it exists and belongs to client
        if (request.SymptomReportId.HasValue)
        {
            var reportExists = await _context.SymptomReports
                .AnyAsync(r => r.Id == request.SymptomReportId.Value && r.ClientId == request.ClientId, cancellationToken);

            if (!reportExists)
                return new CreateAppointmentResult(false, "Rapport de symptômes introuvable ou accès refusé", null);

            var reportAlreadyBooked = await _context.Appointments
                .AnyAsync(a => a.SymptomReportId == request.SymptomReportId.Value, cancellationToken);

            if (reportAlreadyBooked)
                return new CreateAppointmentResult(false, "Un rendez-vous existe déjà pour ce rapport.", null);
        }

        var appointment = new Appointment
        {
            ClientId = request.ClientId,
            VehicleId = request.VehicleId,
            GarageId = request.GarageId,
            SymptomReportId = request.SymptomReportId,
            PreferredDate = request.PreferredDate,
            PreferredTime = request.PreferredTime,
            SpecialRequests = request.SpecialRequests,
            Status = AppointmentStatus.Pending
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateAppointmentResult(true, "Rendez-vous créé avec succès", appointment.Id);
    }
}

