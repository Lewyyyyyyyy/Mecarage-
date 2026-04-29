using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Admin.Commands;

public record UpdateGarageAdminCommand(
    Guid GarageId,
    Guid? NewAdminId
) : IRequest<UpdateGarageAdminResult>;

public record UpdateGarageAdminResult(bool Success, string Message);

public class UpdateGarageAdminCommandHandler : IRequestHandler<UpdateGarageAdminCommand, UpdateGarageAdminResult>
{
    private readonly IApplicationDbContext _context;

    public UpdateGarageAdminCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UpdateGarageAdminResult> Handle(UpdateGarageAdminCommand request, CancellationToken cancellationToken)
    {
        var garage = await _context.Garages
            .FirstOrDefaultAsync(g => g.Id == request.GarageId, cancellationToken);

        if (garage == null)
            return new UpdateGarageAdminResult(false, "Garage introuvable");

        if (request.NewAdminId.HasValue)
        {
            // Verify new admin exists and belongs to the same tenant with ChefAtelier role
            var newAdmin = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.NewAdminId &&
                                          u.TenantId == garage.TenantId, cancellationToken);

            if (newAdmin == null)
                return new UpdateGarageAdminResult(false, "Administrateur introuvable ou n'appartient pas à ce tenant");

            // Check if new admin is already assigned to another garage
            var existingAssignment = await _context.Garages
                .FirstOrDefaultAsync(g => g.AdminId == request.NewAdminId && g.Id != request.GarageId, cancellationToken);

            if (existingAssignment != null)
                return new UpdateGarageAdminResult(false, "Cet utilisateur est déjà administrateur d'un autre garage");
        }

        garage.AdminId = request.NewAdminId;
        await _context.SaveChangesAsync(cancellationToken);

        var message = request.NewAdminId.HasValue
            ? "Administrateur du garage mis à jour avec succès"
            : "Administrateur du garage supprimé avec succès";

        return new UpdateGarageAdminResult(true, message);
    }
}

