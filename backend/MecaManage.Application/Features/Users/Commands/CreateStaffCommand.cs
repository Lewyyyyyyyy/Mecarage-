using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Entities;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Users.Commands;

public record CreateStaffCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string Phone,
    string Role,
    Guid GarageId
) : IRequest<CreateStaffResult>;

public record CreateStaffResult(bool Success, string Message, Guid? UserId);

public class CreateStaffCommandHandler : IRequestHandler<CreateStaffCommand, CreateStaffResult>
{
    private readonly IApplicationDbContext _context;

    public CreateStaffCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CreateStaffResult> Handle(CreateStaffCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role) ||
            (role != UserRole.ChefAtelier && role != UserRole.Mecanicien))
        {
            return new CreateStaffResult(false, "Rôle invalide", null);
        }

        var garage = await _context.Garages
            .FirstOrDefaultAsync(g => g.Id == request.GarageId, cancellationToken);

        if (garage == null)
            return new CreateStaffResult(false, "Garage introuvable", null);

        if (!garage.IsActive)
            return new CreateStaffResult(false, "Garage désactivé", null);

        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == request.Email, cancellationToken);

        if (emailExists)
            return new CreateStaffResult(false, "Email déjà utilisé", null);

        var staff = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Phone = request.Phone,
            Role = role,
            GarageId = request.GarageId,
            TenantId = garage.TenantId,
            IsActive = true
        };

        _context.Users.Add(staff);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateStaffResult(true, $"{role} créé avec succès", staff.Id);
    }
}

