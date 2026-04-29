using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Admin.Queries;

public record GetGarageAdminsQuery(Guid TenantId) : IRequest<GetGarageAdminsResult>;

public record GarageAdminDto(
    Guid GarageId,
    string GarageName,
    Guid? AdminId,
    string? AdminFirstName,
    string? AdminLastName,
    string? AdminEmail,
    string? AdminPhone,
    bool HasAdmin
);

public record GetGarageAdminsResult(bool Success, string Message, List<GarageAdminDto> Data);

public class GetGarageAdminsQueryHandler : IRequestHandler<GetGarageAdminsQuery, GetGarageAdminsResult>
{
    private readonly IApplicationDbContext _context;

    public GetGarageAdminsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GetGarageAdminsResult> Handle(GetGarageAdminsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var garages = await _context.Garages
                .Where(g => g.TenantId == request.TenantId)
                .Include(g => g.Admin)
                .OrderBy(g => g.Name)
                .Select(g => new GarageAdminDto(
                    g.Id,
                    g.Name,
                    g.AdminId,
                    g.Admin != null ? g.Admin.FirstName : null,
                    g.Admin != null ? g.Admin.LastName : null,
                    g.Admin != null ? g.Admin.Email : null,
                    g.Admin != null ? g.Admin.Phone : null,
                    g.AdminId.HasValue
                ))
                .ToListAsync(cancellationToken);

            return new GetGarageAdminsResult(true, "Administrateurs de garage récupérés avec succès", garages);
        }
        catch (Exception ex)
        {
            return new GetGarageAdminsResult(false, $"Erreur lors de la récupération des administrateurs: {ex.Message}", new List<GarageAdminDto>());
        }
    }
}

