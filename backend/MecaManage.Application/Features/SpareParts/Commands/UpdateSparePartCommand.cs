using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.SpareParts.Commands;

public record UpdateSparePartCommand(
    Guid PartId,
    Guid GarageId,
    Guid RequestingUserId,
    string Name,
    string Description,
    string Category,
    decimal UnitPrice,
    int ReorderLevel,
    string? Manufacturer,
    string? PartNumber,
    bool IsActive
) : IRequest<UpdateSparePartResult>;

public record UpdateSparePartResult(bool Success, string Message);

public class UpdateSparePartCommandHandler : IRequestHandler<UpdateSparePartCommand, UpdateSparePartResult>
{
    private readonly IApplicationDbContext _context;

    public UpdateSparePartCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UpdateSparePartResult> Handle(UpdateSparePartCommand request, CancellationToken cancellationToken)
    {
        var part = await _context.SpareParts
            .FirstOrDefaultAsync(sp => sp.Id == request.PartId && sp.GarageId == request.GarageId, cancellationToken);

        if (part == null)
            return new UpdateSparePartResult(false, "Pièce introuvable dans ce garage.");

        // Verify caller belongs to this garage
        var garage = await _context.Garages.FirstOrDefaultAsync(g => g.Id == request.GarageId, cancellationToken);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.RequestingUserId, cancellationToken);
        bool belongs = user?.GarageId == request.GarageId || garage?.AdminId == request.RequestingUserId;
        if (!belongs)
            return new UpdateSparePartResult(false, "Accès refusé à ce garage.");

        part.Name = request.Name.Trim();
        part.Description = request.Description.Trim();
        part.Category = request.Category.Trim();
        part.UnitPrice = request.UnitPrice;
        part.ReorderLevel = request.ReorderLevel;
        part.Manufacturer = request.Manufacturer?.Trim();
        part.PartNumber = request.PartNumber?.Trim();
        part.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        return new UpdateSparePartResult(true, "Pièce mise à jour.");
    }
}

