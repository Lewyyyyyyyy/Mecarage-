using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Entities;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Admin.Commands;

public record CreateGarageAdminCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string Phone,
    Guid TenantId,
    Guid GarageId
) : IRequest<CreateGarageAdminResult>;

public record CreateGarageAdminResult(bool Success, string Message, Guid? AdminId);

public class CreateGarageAdminCommandHandler : IRequestHandler<CreateGarageAdminCommand, CreateGarageAdminResult>
{
    private readonly IApplicationDbContext _context;

    public CreateGarageAdminCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CreateGarageAdminResult> Handle(CreateGarageAdminCommand request, CancellationToken cancellationToken)
    {
        // Verify tenant exists
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant == null)
            return new CreateGarageAdminResult(false, "Tenant introuvable", null);

        if (!tenant.IsActive)
            return new CreateGarageAdminResult(false, "Tenant désactivé", null);

        // Verify garage exists and belongs to tenant
        var garage = await _context.Garages
            .FirstOrDefaultAsync(g => g.Id == request.GarageId && g.TenantId == request.TenantId, cancellationToken);

        if (garage == null)
            return new CreateGarageAdminResult(false, "Garage introuvable ou n'appartient pas à ce tenant", null);

        // Check if garage already has an admin
        if (garage.AdminId.HasValue)
            return new CreateGarageAdminResult(false, "Ce garage a déjà un administrateur assigné", null);

        // Check if email already exists
        var existingUser = await _context.Users
            .AnyAsync(u => u.Email == request.Email, cancellationToken);

        if (existingUser)
            return new CreateGarageAdminResult(false, "Email déjà utilisé", null);

        // Create the admin user
        var admin = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Phone = request.Phone,
            Role = UserRole.AdminEntreprise,  // Garage manager admin role - manages garage overall stats and operations
            TenantId = request.TenantId,
            GarageId = request.GarageId,
            IsActive = true
        };

        _context.Users.Add(admin);

        // Assign admin to garage
        garage.AdminId = admin.Id;

        await _context.SaveChangesAsync(cancellationToken);

        return new CreateGarageAdminResult(true, "Administrateur de garage créé avec succès", admin.Id);
    }
}

