using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Users.Commands;

public record DeleteStaffCommand(Guid UserId) : IRequest<DeleteStaffResult>;

public record DeleteStaffResult(bool Success, string Message);

public class DeleteStaffCommandHandler : IRequestHandler<DeleteStaffCommand, DeleteStaffResult>
{
    private readonly IApplicationDbContext _context;

    public DeleteStaffCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DeleteStaffResult> Handle(DeleteStaffCommand request, CancellationToken cancellationToken)
    {
        var staff = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (staff == null)
            return new DeleteStaffResult(false, "Utilisateur introuvable");

        if (staff.Role != UserRole.ChefAtelier && staff.Role != UserRole.Mecanicien)
            return new DeleteStaffResult(false, "Ce compte ne peut pas être supprimé depuis la gestion du personnel");

        var garage = await _context.Garages
            .FirstOrDefaultAsync(g => g.AdminId == staff.Id, cancellationToken);

        if (garage != null)
            garage.AdminId = null;

        staff.GarageId = null;
        staff.IsActive = false;
        staff.IsDeleted = true;
        staff.RefreshToken = null;
        staff.RefreshTokenExpiry = null;

        await _context.SaveChangesAsync(cancellationToken);

        return new DeleteStaffResult(true, "Personnel supprimé avec succès");
    }
}

