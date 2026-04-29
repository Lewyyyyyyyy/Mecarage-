using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Interventions.Commands;

public record AssignMecanicienCommand(
    Guid InterventionId,
    Guid MecanicienId
) : IRequest<AssignMecanicienResult>;

public record AssignMecanicienResult(bool Success, string Message);

public class AssignMecanicienCommandHandler : IRequestHandler<AssignMecanicienCommand, AssignMecanicienResult>
{
    private readonly IApplicationDbContext _context;

    public AssignMecanicienCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AssignMecanicienResult> Handle(AssignMecanicienCommand request, CancellationToken cancellationToken)
    {
        var intervention = await _context.InterventionRequests
            .FirstOrDefaultAsync(i => i.Id == request.InterventionId, cancellationToken);

        if (intervention == null)
            return new AssignMecanicienResult(false, "Intervention introuvable");

        var mecanicien = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.MecanicienId && u.Role == UserRole.Mecanicien, cancellationToken);

        if (mecanicien == null)
            return new AssignMecanicienResult(false, "Mécanicien introuvable");

        intervention.AssignedMecanicienId = request.MecanicienId;
        intervention.Status = InterventionStatus.EnCours;
        intervention.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new AssignMecanicienResult(true, $"Mécanicien {mecanicien.FirstName} {mecanicien.LastName} assigné");
    }
}