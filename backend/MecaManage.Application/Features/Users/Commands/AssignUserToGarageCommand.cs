using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Users.Commands;

public record AssignUserToGarageCommand(
    Guid UserId,
    Guid GarageId
) : IRequest<AssignUserToGarageResult>;

public record AssignUserToGarageResult(bool Success, string Message);

public class AssignUserToGarageCommandHandler : IRequestHandler<AssignUserToGarageCommand, AssignUserToGarageResult>
{
    private readonly IApplicationDbContext _context;

    public AssignUserToGarageCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AssignUserToGarageResult> Handle(AssignUserToGarageCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null)
            return new AssignUserToGarageResult(false, "Utilisateur introuvable");

        var garage = await _context.Garages.FirstOrDefaultAsync(g => g.Id == request.GarageId, cancellationToken);
        if (garage == null)
            return new AssignUserToGarageResult(false, "Garage introuvable");

        // Only allow assigning to garage if user is AdminEntreprise or ChefAtelier
        if (user.Role != Domain.Enums.UserRole.AdminEntreprise && user.Role != Domain.Enums.UserRole.ChefAtelier)
            return new AssignUserToGarageResult(false, "Seuls les AdminEntreprise et ChefAtelier peuvent être assignés à un garage");

        user.GarageId = request.GarageId;
        await _context.SaveChangesAsync(cancellationToken);

        return new AssignUserToGarageResult(true, $"Utilisateur assigné au garage avec succès");
    }
}

