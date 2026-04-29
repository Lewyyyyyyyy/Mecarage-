using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Garages.Queries;

public record GetGaragesQuery(Guid TenantId) : IRequest<List<GarageDto>>;

/// <summary>
/// Data Transfer Object representing a garage (repair workshop).
/// </summary>
/// <param name="Id">The unique identifier (GUID) of the garage.</param>
/// <param name="TenantId">The ID of the tenant (company) this garage belongs to.</param>
/// <param name="Name">The name of the garage.</param>
/// <param name="Address">The street address of the garage.</param>
/// <param name="City">The city where the garage is located.</param>
/// <param name="Phone">The contact phone number for the garage.</param>
/// <param name="Latitude">The geographic latitude coordinate of the garage (optional).</param>
/// <param name="Longitude">The geographic longitude coordinate of the garage (optional).</param>
/// <param name="IsActive">Whether the garage is currently active and operational.</param>
/// <param name="AdminId">The ID of the garage admin (optional).</param>
/// <param name="AdminFirstName">The first name of the garage admin (optional).</param>
/// <param name="AdminLastName">The last name of the garage admin (optional).</param>
public record GarageDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string Address,
    string City,
    string Phone,
    double? Latitude,
    double? Longitude,
    bool IsActive,
    Guid? AdminId = null,
    string? AdminFirstName = null,
    string? AdminLastName = null
);

public class GetGaragesQueryHandler : IRequestHandler<GetGaragesQuery, List<GarageDto>>
{
    private readonly IApplicationDbContext _context;

    public GetGaragesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<GarageDto>> Handle(GetGaragesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Garages
            .Where(g => g.TenantId == request.TenantId)
            .Include(g => g.Admin)
            .Select(g => new GarageDto(
                g.Id,
                g.TenantId,
                g.Name,
                g.Address,
                g.City,
                g.Phone,
                g.Latitude,
                g.Longitude,
                g.IsActive,
                g.AdminId,
                g.Admin != null ? g.Admin.FirstName : null,
                g.Admin != null ? g.Admin.LastName : null
            ))
            .ToListAsync(cancellationToken);
    }
}