using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.InterventionLifecycle.Queries;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record InterventionSummaryDto(
    Guid   Id,
    string Status,
    bool?  ProceedWithIntervention,
    string ClientName,
    string VehicleInfo,
    string GarageName,
    string? InvoiceNumber,
    decimal? PaymentAmount,
    string? PaymentMethod,
    DateTime? PaymentDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record InterventionDetailDto(
    Guid    Id,
    string  Status,
    bool?   ProceedWithIntervention,
    // Identifiers
    Guid   TenantId,
    Guid   GarageId,
    Guid   ClientId,
    Guid   VehicleId,
    Guid?  AppointmentId,
    Guid?  SymptomReportId,
    Guid?  InvoiceId,
    Guid?  RepairTaskId,
    // Resolved names
    string ClientName,
    string ClientEmail,
    string VehicleInfo,
    string GarageName,
    // Examination
    string? ExaminationNotes,
    string? PartsUsedJson,
    // Repair
    string? RepairNotes,
    // Invoice
    string?  InvoiceNumber,
    decimal? InvoiceTotal,
    string?  InvoiceStatus,
    // Payment
    decimal?  PaymentAmount,
    string?   PaymentMethod,
    DateTime? PaymentDate,
    string?   PaidBy,
    // Timestamps
    DateTime  CreatedAt,
    DateTime? UpdatedAt
);

// ── Queries ───────────────────────────────────────────────────────────────────

public record GetGarageInterventionsQuery(Guid GarageId)
    : IRequest<List<InterventionSummaryDto>>;

public record GetClientInterventionsQuery(Guid ClientId)
    : IRequest<List<InterventionSummaryDto>>;

public record GetInterventionDetailQuery(Guid InterventionId)
    : IRequest<InterventionDetailDto?>;

public record GetInterventionByAppointmentQuery(Guid AppointmentId)
    : IRequest<InterventionDetailDto?>;

// ── Handlers ─────────────────────────────────────────────────────────────────

public class GetGarageInterventionsHandler
    : IRequestHandler<GetGarageInterventionsQuery, List<InterventionSummaryDto>>
{
    private readonly IApplicationDbContext _context;
    public GetGarageInterventionsHandler(IApplicationDbContext context) => _context = context;

    public async Task<List<InterventionSummaryDto>> Handle(
        GetGarageInterventionsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Interventions
            .Include(i => i.Client)
            .Include(i => i.Vehicle)
            .Include(i => i.Garage)
            .Include(i => i.Invoice)
            .Where(i => i.GarageId == request.GarageId)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new InterventionSummaryDto(
                i.Id,
                i.Status.ToString(),
                i.ProceedWithIntervention,
                i.Client.FirstName + " " + i.Client.LastName,
                i.Vehicle.Brand + " " + i.Vehicle.Model + " (" + i.Vehicle.LicensePlate + ")",
                i.Garage.Name,
                i.Invoice != null ? i.Invoice.InvoiceNumber : null,
                i.PaymentAmount,
                i.PaymentMethod,
                i.PaymentDate,
                i.CreatedAt,
                i.UpdatedAt
            ))
            .ToListAsync(cancellationToken);
    }
}

public class GetClientInterventionsHandler
    : IRequestHandler<GetClientInterventionsQuery, List<InterventionSummaryDto>>
{
    private readonly IApplicationDbContext _context;
    public GetClientInterventionsHandler(IApplicationDbContext context) => _context = context;

    public async Task<List<InterventionSummaryDto>> Handle(
        GetClientInterventionsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Interventions
            .Include(i => i.Client)
            .Include(i => i.Vehicle)
            .Include(i => i.Garage)
            .Include(i => i.Invoice)
            .Where(i => i.ClientId == request.ClientId)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new InterventionSummaryDto(
                i.Id,
                i.Status.ToString(),
                i.ProceedWithIntervention,
                i.Client.FirstName + " " + i.Client.LastName,
                i.Vehicle.Brand + " " + i.Vehicle.Model + " (" + i.Vehicle.LicensePlate + ")",
                i.Garage.Name,
                i.Invoice != null ? i.Invoice.InvoiceNumber : null,
                i.PaymentAmount,
                i.PaymentMethod,
                i.PaymentDate,
                i.CreatedAt,
                i.UpdatedAt
            ))
            .ToListAsync(cancellationToken);
    }
}

public class GetInterventionDetailHandler
    : IRequestHandler<GetInterventionDetailQuery, InterventionDetailDto?>
{
    private readonly IApplicationDbContext _context;
    public GetInterventionDetailHandler(IApplicationDbContext context) => _context = context;

    public Task<InterventionDetailDto?> Handle(
        GetInterventionDetailQuery request, CancellationToken cancellationToken)
        => BuildQuery(_context, cancellationToken, request.InterventionId, null);

    internal static async Task<InterventionDetailDto?> BuildQuery(
        IApplicationDbContext context,
        CancellationToken ct,
        Guid? byId,
        Guid? byAppointmentId)
    {
        var q = context.Interventions
            .Include(i => i.Client)
            .Include(i => i.Vehicle)
            .Include(i => i.Garage)
            .Include(i => i.Invoice)
            .AsQueryable();

        if (byId.HasValue)         q = q.Where(i => i.Id == byId.Value);
        if (byAppointmentId.HasValue) q = q.Where(i => i.AppointmentId == byAppointmentId.Value);

        return await q
            .Select(i => new InterventionDetailDto(
                i.Id,
                i.Status.ToString(),
                i.ProceedWithIntervention,
                i.TenantId,
                i.GarageId,
                i.ClientId,
                i.VehicleId,
                i.AppointmentId,
                i.SymptomReportId,
                i.InvoiceId,
                i.RepairTaskId,
                i.Client.FirstName + " " + i.Client.LastName,
                i.Client.Email,
                i.Vehicle.Brand + " " + i.Vehicle.Model + " (" + i.Vehicle.LicensePlate + ")",
                i.Garage.Name,
                i.ExaminationNotes,
                i.PartsUsedJson,
                i.RepairNotes,
                i.Invoice != null ? i.Invoice.InvoiceNumber : null,
                i.Invoice != null ? (decimal?)i.Invoice.TotalAmount : null,
                i.Invoice != null ? i.Invoice.Status.ToString() : null,
                i.PaymentAmount,
                i.PaymentMethod,
                i.PaymentDate,
                i.PaidBy,
                i.CreatedAt,
                i.UpdatedAt
            ))
            .FirstOrDefaultAsync(ct);
    }
}

public class GetInterventionByAppointmentHandler
    : IRequestHandler<GetInterventionByAppointmentQuery, InterventionDetailDto?>
{
    private readonly IApplicationDbContext _context;
    public GetInterventionByAppointmentHandler(IApplicationDbContext context) => _context = context;

    public Task<InterventionDetailDto?> Handle(
        GetInterventionByAppointmentQuery request, CancellationToken cancellationToken)
        => GetInterventionDetailHandler.BuildQuery(_context, cancellationToken, null, request.AppointmentId);
}

