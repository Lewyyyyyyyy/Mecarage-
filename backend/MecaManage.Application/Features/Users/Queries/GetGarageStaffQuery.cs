using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Users.Queries;

public record GetGarageStaffQuery(Guid GarageId) : IRequest<List<StaffDto>>;

public record StaffDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Role,
    Guid? GarageId,
    bool IsActive
);

public class GetGarageStaffQueryHandler : IRequestHandler<GetGarageStaffQuery, List<StaffDto>>
{
    private readonly IApplicationDbContext _context;

    public GetGarageStaffQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<StaffDto>> Handle(GetGarageStaffQuery request, CancellationToken cancellationToken)
    {
        return await _context.Users
            .Where(u => u.GarageId == request.GarageId &&
                        (u.Role == UserRole.ChefAtelier || u.Role == UserRole.Mecanicien))
            .OrderBy(u => u.Role)
            .ThenBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Select(u => new StaffDto(
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.Phone,
                u.Role.ToString(),
                u.GarageId,
                u.IsActive
            ))
            .ToListAsync(cancellationToken);
    }
}

