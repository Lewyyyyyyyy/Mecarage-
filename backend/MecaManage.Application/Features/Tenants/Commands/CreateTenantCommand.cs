using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Tenants.Commands;

/// <summary>
/// Command to create a new tenant (company/garage organization).
/// </summary>
/// <param name="Name">The name of the tenant (e.g., "Main Garage"). Must be at least 3 characters.</param>
/// <param name="Slug">Unique URL-friendly identifier (e.g., "main-garage"). Must be alphanumeric with hyphens.</param>
/// <param name="Email">Contact email address. Must be unique across all tenants.</param>
/// <param name="Phone">Contact phone number.</param>
public record CreateTenantCommand(
    string Name,
    string Slug,
    string Email,
    string Phone
) : IRequest<CreateTenantResult>;

/// <summary>
/// Result of creating a tenant.
/// </summary>
/// <param name="Success">Indicates if the tenant was created successfully.</param>
/// <param name="Message">Descriptive message about the result.</param>
/// <param name="TenantId">The ID of the newly created tenant (null if creation failed).</param>
public record CreateTenantResult(bool Success, string Message, Guid? TenantId);

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, CreateTenantResult>
{
    private readonly IApplicationDbContext _context;

    public CreateTenantCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CreateTenantResult> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var exists = await _context.Tenants
            .AnyAsync(t => t.Slug == request.Slug || t.Email == request.Email, cancellationToken);

        if (exists)
            return new CreateTenantResult(false, "Slug ou Email déjà utilisé", null);

        var tenant = new Tenant
        {
            Name = request.Name,
            Slug = request.Slug,
            Email = request.Email,
            Phone = request.Phone,
            IsActive = true
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateTenantResult(true, "Tenant créé avec succès", tenant.Id);
    }
}