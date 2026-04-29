using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Tenants.Queries;

public record GetTenantByIdQuery(Guid Id) : IRequest<TenantDto?>;

public class GetTenantByIdQueryHandler : IRequestHandler<GetTenantByIdQuery, TenantDto?>
{
    private readonly IApplicationDbContext _context;

    public GetTenantByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TenantDto?> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants
            .Where(t => t.Id == request.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (tenant == null)
            return null;

        var garageCount = await _context.Garages
            .CountAsync(g => g.TenantId == request.Id, cancellationToken);

        var userCount = await _context.Users
            .CountAsync(u => u.TenantId == request.Id, cancellationToken);

        return new TenantDto(
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.Email,
            tenant.Phone,
            tenant.IsActive,
            tenant.CreatedAt,
            garageCount,
            userCount
        );
    }
}

