using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.SpareParts.Commands;

public record CreateSparePartCommand(
    Guid GarageId,
    Guid RequestingUserId,
    string Code,
    string Name,
    string Description,
    string Category,
    decimal UnitPrice,
    int StockQuantity,
    int ReorderLevel,
    string? Manufacturer,
    string? PartNumber
) : IRequest<CreateSparePartResult>;

public record CreateSparePartResult(bool Success, string Message, Guid? PartId);

public class CreateSparePartCommandHandler : IRequestHandler<CreateSparePartCommand, CreateSparePartResult>
{
    private readonly IApplicationDbContext _context;

    public CreateSparePartCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CreateSparePartResult> Handle(CreateSparePartCommand request, CancellationToken cancellationToken)
    {
        // Verify garage exists
        var garage = await _context.Garages.FirstOrDefaultAsync(g => g.Id == request.GarageId, cancellationToken);
        if (garage == null)
            return new CreateSparePartResult(false, "Garage introuvable.", null);

        // Verify caller belongs to this garage
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.RequestingUserId, cancellationToken);
        if (user == null)
            return new CreateSparePartResult(false, "Utilisateur introuvable.", null);

        bool belongs = user.GarageId == request.GarageId || garage.AdminId == request.RequestingUserId;
        if (!belongs)
            return new CreateSparePartResult(false, "Accès refusé à ce garage.", null);

        // Code must be unique within this garage
        var codeExists = await _context.SpareParts
            .AnyAsync(sp => sp.GarageId == request.GarageId && sp.Code == request.Code, cancellationToken);
        if (codeExists)
            return new CreateSparePartResult(false, $"Le code '{request.Code}' existe déjà dans ce garage.", null);

        var part = new SparePart
        {
            GarageId = request.GarageId,
            Code = request.Code.Trim().ToUpper(),
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category.Trim(),
            UnitPrice = request.UnitPrice,
            StockQuantity = request.StockQuantity,
            ReorderLevel = request.ReorderLevel,
            Manufacturer = request.Manufacturer?.Trim(),
            PartNumber = request.PartNumber?.Trim(),
            IsActive = true,
            LastRestocked = DateTime.UtcNow,
        };

        _context.SpareParts.Add(part);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateSparePartResult(true, "Pièce créée avec succès.", part.Id);
    }
}

