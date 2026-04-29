using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Tenants.Queries;

public record GetTenantsQuery : IRequest<List<TenantDto>>;

/// <summary>
/// Data Transfer Object representing a tenant with associated counts.
/// </summary>
/// <param name="Id">The unique identifier (GUID) of the tenant.</param>
/// <param name="Name">The name of the tenant/company.</param>
/// <param name="Slug">URL-friendly identifier for the tenant.</param>
/// <param name="Email">Contact email address for the tenant.</param>
/// <param name="Phone">Contact phone number for the tenant.</param>
/// <param name="IsActive">Whether the tenant is currently active.</param>
/// <param name="CreatedAt">The date and time when the tenant was created.</param>
/// <param name="GarageCount">Number of garages associated with this tenant. Defaults to 0.</param>
/// <param name="UserCount">Number of users associated with this tenant. Defaults to 0.</param>
public record TenantDto(
    Guid Id,
    string Name,
    string Slug,
    string Email,
    string Phone,
    bool IsActive,
    DateTime CreatedAt,
    int GarageCount = 0,
    int UserCount = 0
);

public class GetTenantsQueryHandler : IRequestHandler<GetTenantsQuery, List<TenantDto>>
{
    private readonly IApplicationDbContext _context;

    public GetTenantsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TenantDto>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
    {
        var tenants = await _context.Tenants.ToListAsync(cancellationToken);

        var result = new List<TenantDto>();
        foreach (var tenant in tenants)
        {
            var garageCount = await _context.Garages
                .CountAsync(g => g.TenantId == tenant.Id, cancellationToken);

            var userCount = await _context.Users
                .CountAsync(u => u.TenantId == tenant.Id, cancellationToken);

            result.Add(new TenantDto(
                tenant.Id,
                tenant.Name,
                tenant.Slug,
                tenant.Email,
                tenant.Phone,
                tenant.IsActive,
                tenant.CreatedAt,
                garageCount,
                userCount
            ));
        }

        return result;
    }
}