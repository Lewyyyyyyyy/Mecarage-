using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Tenants.Commands;

public record DeleteTenantCommand(Guid Id) : IRequest<DeleteTenantResult>;

public record DeleteTenantResult(bool Success, string Message);

public class DeleteTenantCommandHandler : IRequestHandler<DeleteTenantCommand, DeleteTenantResult>
{
    private readonly IApplicationDbContext _context;

    public DeleteTenantCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DeleteTenantResult> Handle(DeleteTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (tenant == null)
            return new DeleteTenantResult(false, "Tenant non trouvé");

        // Check if tenant has any garages
        var hasGarages = await _context.Garages.AnyAsync(g => g.TenantId == request.Id, cancellationToken);
        if (hasGarages)
            return new DeleteTenantResult(false, "Impossible de supprimer un tenant avec des garages");

        _context.Tenants.Remove(tenant);
        await _context.SaveChangesAsync(cancellationToken);

        return new DeleteTenantResult(true, "Tenant supprimé avec succès");
    }
}

