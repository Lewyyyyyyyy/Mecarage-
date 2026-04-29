using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Interventions.Queries;

public record GetInterventionsQuery() : IRequest<List<InterventionDto>>;

public record InterventionDto(
    Guid Id, Guid ClientId, Guid VehicleId, Guid GarageId,
    string Description, string Status, string UrgencyLevel,
    DateTime? AppointmentDate, DateTime CreatedAt
);

public class GetInterventionsQueryHandler : IRequestHandler<GetInterventionsQuery, List<InterventionDto>>
{
    private readonly IApplicationDbContext _context;

    public GetInterventionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<InterventionDto>> Handle(GetInterventionsQuery request, CancellationToken cancellationToken)
    {
        return await _context.InterventionRequests
            .Select(i => new InterventionDto(
                i.Id, i.ClientId, i.VehicleId, i.GarageId,
                i.Description, i.Status.ToString(), i.UrgencyLevel.ToString(),
                i.AppointmentDate, i.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}