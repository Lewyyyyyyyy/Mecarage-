using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Tenants.Commands;

/// <summary>
/// Command to update an existing tenant's information.
/// </summary>
/// <param name="Id">The ID of the tenant to update.</param>
/// <param name="Name">The new name of the tenant.</param>
/// <param name="Email">The new email address (must be unique). Optional if not changing.</param>
/// <param name="Phone">The new phone number.</param>
public record UpdateTenantCommand(
    Guid Id,
    string Name,
    string Email,
    string Phone
) : IRequest<UpdateTenantResult>;

/// <summary>
/// Result of updating a tenant.
/// </summary>
/// <param name="Success">Indicates if the tenant was updated successfully.</param>
/// <param name="Message">Descriptive message about the result.</param>
public record UpdateTenantResult(bool Success, string Message);

public class UpdateTenantCommandHandler : IRequestHandler<UpdateTenantCommand, UpdateTenantResult>
{
    private readonly IApplicationDbContext _context;

    public UpdateTenantCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UpdateTenantResult> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (tenant == null)
            return new UpdateTenantResult(false, "Tenant non trouvé");

        // Check if email is already used by another tenant
        var emailExists = await _context.Tenants
            .AnyAsync(t => t.Email == request.Email && t.Id != request.Id, cancellationToken);

        if (emailExists)
            return new UpdateTenantResult(false, "Email déjà utilisé");

        tenant.Name = request.Name;
        tenant.Email = request.Email;
        tenant.Phone = request.Phone;

        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync(cancellationToken);

        return new UpdateTenantResult(true, "Tenant mis à jour avec succès");
    }
}

