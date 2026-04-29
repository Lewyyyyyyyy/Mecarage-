using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Interventions.Commands;

public record UpdateInterventionStatusCommand(
    Guid InterventionId,
    InterventionStatus NewStatus,
    string? Notes
) : IRequest<UpdateInterventionStatusResult>;

public record UpdateInterventionStatusResult(bool Success, string Message);

public class UpdateInterventionStatusCommandHandler : IRequestHandler<UpdateInterventionStatusCommand, UpdateInterventionStatusResult>
{
    private readonly IApplicationDbContext _context;

    public UpdateInterventionStatusCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UpdateInterventionStatusResult> Handle(UpdateInterventionStatusCommand request, CancellationToken cancellationToken)
    {
        var intervention = await _context.InterventionRequests
            .FirstOrDefaultAsync(i => i.Id == request.InterventionId, cancellationToken);

        if (intervention == null)
            return new UpdateInterventionStatusResult(false, "Intervention introuvable");

        intervention.Status = request.NewStatus;
        if (request.Notes != null)
            intervention.Notes = request.Notes;
        intervention.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new UpdateInterventionStatusResult(true, $"Statut mis à jour : {request.NewStatus}");
    }
}