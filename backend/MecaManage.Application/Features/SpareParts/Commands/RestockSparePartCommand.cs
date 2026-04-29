using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.SpareParts.Commands;

public record RestockSparePartCommand(
    Guid PartId,
    Guid GarageId,
    Guid RequestingUserId,
    int QuantityToAdd
) : IRequest<RestockSparePartResult>;

public record RestockSparePartResult(bool Success, string Message, int NewQuantity);

public class RestockSparePartCommandHandler : IRequestHandler<RestockSparePartCommand, RestockSparePartResult>
{
    private readonly IApplicationDbContext _context;

    public RestockSparePartCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RestockSparePartResult> Handle(RestockSparePartCommand request, CancellationToken cancellationToken)
    {
        if (request.QuantityToAdd <= 0)
            return new RestockSparePartResult(false, "La quantité doit être supérieure à 0.", 0);

        var part = await _context.SpareParts
            .FirstOrDefaultAsync(sp => sp.Id == request.PartId && sp.GarageId == request.GarageId, cancellationToken);

        if (part == null)
            return new RestockSparePartResult(false, "Pièce introuvable dans ce garage.", 0);

        // Verify caller belongs to this garage
        var garage = await _context.Garages.FirstOrDefaultAsync(g => g.Id == request.GarageId, cancellationToken);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.RequestingUserId, cancellationToken);
        bool belongs = user?.GarageId == request.GarageId || garage?.AdminId == request.RequestingUserId;
        if (!belongs)
            return new RestockSparePartResult(false, "Accès refusé à ce garage.", 0);

        part.StockQuantity += request.QuantityToAdd;
        part.LastRestocked = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return new RestockSparePartResult(true, $"+{request.QuantityToAdd} unités ajoutées.", part.StockQuantity);
    }
}

