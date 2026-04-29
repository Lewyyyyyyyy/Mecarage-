using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.SpareParts.Commands;

public record DeleteSparePartCommand(
    Guid PartId,
    Guid GarageId,
    Guid RequestingUserId
) : IRequest<DeleteSparePartResult>;

public record DeleteSparePartResult(bool Success, string Message);

public class DeleteSparePartCommandHandler : IRequestHandler<DeleteSparePartCommand, DeleteSparePartResult>
{
    private readonly IApplicationDbContext _context;

    public DeleteSparePartCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DeleteSparePartResult> Handle(DeleteSparePartCommand request, CancellationToken cancellationToken)
    {
        var part = await _context.SpareParts
            .FirstOrDefaultAsync(sp => sp.Id == request.PartId && sp.GarageId == request.GarageId, cancellationToken);

        if (part == null)
            return new DeleteSparePartResult(false, "Pièce introuvable dans ce garage.");

        // Verify caller belongs to this garage
        var garage = await _context.Garages.FirstOrDefaultAsync(g => g.Id == request.GarageId, cancellationToken);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.RequestingUserId, cancellationToken);
        bool belongs = user?.GarageId == request.GarageId || garage?.AdminId == request.RequestingUserId;
        if (!belongs)
            return new DeleteSparePartResult(false, "Accès refusé à ce garage.");

        // Soft delete
        part.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
        return new DeleteSparePartResult(true, "Pièce supprimée.");
    }
}

