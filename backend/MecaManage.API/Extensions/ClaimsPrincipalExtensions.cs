using System.Security.Claims;

namespace MecaManage.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    public static Guid GetTenantId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst("tenantId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    public static Guid? GetGarageId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst("garageId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    public static string GetRole(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    }

    public static string GetUserFullName(this ClaimsPrincipal user)
    {
        var name = user.FindFirst(ClaimTypes.Name)?.Value
                ?? user.FindFirst("name")?.Value
                ?? user.FindFirst(ClaimTypes.Email)?.Value
                ?? "Admin";
        return name;
    }
}