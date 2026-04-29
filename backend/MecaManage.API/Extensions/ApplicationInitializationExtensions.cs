using MecaManage.Domain.Entities;
using MecaManage.Domain.Enums;
using MecaManage.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.API.Extensions;

public static class ApplicationInitializationExtensions
{
    public static async Task EnsurePlatformSuperAdminAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var configuration = services.GetRequiredService<IConfiguration>();
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        var logger = services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("SuperAdminBootstrap");

        var section = configuration.GetSection("BootstrapSuperAdmin");
        var enabled = section.GetValue<bool>("Enabled");
        if (!enabled)
        {
            logger.LogInformation("SuperAdmin bootstrap is disabled.");
            return;
        }

        var email = section["Email"]?.Trim().ToLowerInvariant();
        var password = section["Password"];
        var firstName = section["FirstName"]?.Trim() ?? "Super";
        var lastName = section["LastName"]?.Trim() ?? "Admin";
        var phone = section["Phone"]?.Trim() ?? "+33000000000";
        var tenantName = section["TenantName"]?.Trim() ?? "MecaManage Platform";
        var tenantSlug = section["TenantSlug"]?.Trim().ToLowerInvariant() ?? "platform";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("SuperAdmin bootstrap skipped: Email and Password are required in BootstrapSuperAdmin section.");
            return;
        }

        var superAdminExists = await dbContext.Users
            .AnyAsync(u => u.Role == UserRole.SuperAdmin && !u.IsDeleted, cancellationToken);

        if (superAdminExists)
        {
            logger.LogInformation("A SuperAdmin account already exists. Bootstrap skipped.");
            return;
        }

        var emailAlreadyUsed = await dbContext.Users
            .AnyAsync(u => u.Email.ToLower() == email, cancellationToken);

        if (emailAlreadyUsed)
        {
            logger.LogWarning("SuperAdmin bootstrap skipped: email {Email} is already used by another account.", email);
            return;
        }

        // Create or get the default tenant
        var tenantSlugLower = tenantSlug.ToLowerInvariant();
        var tenant = await dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Slug == tenantSlugLower, cancellationToken);

        if (tenant == null)
        {
            tenant = new Tenant
            {
                Name = tenantName,
                Slug = tenantSlugLower,
                Email = email,
                Phone = phone,
                IsActive = true
            };

            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Default tenant '{TenantName}' created.", tenantName);
        }
        else
        {
            logger.LogInformation("Default tenant '{TenantName}' already exists.", tenantName);
        }

        // Create SuperAdmin user and associate with tenant
        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Phone = phone,
            Role = UserRole.SuperAdmin,
            IsActive = true,
            TenantId = tenant.Id  // Associate with tenant
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("SuperAdmin account created successfully with email {Email} and tenant {TenantName}.", email, tenantName);
    }
}
