using System.Security.Claims;

namespace DevNetControl.Api.Infrastructure.Security;

public static class ClaimsHelper
{
    public static Guid GetCurrentUserId(ClaimsPrincipal user)
    {
        return Guid.Parse(user.FindFirst("UserId")!.Value);
    }

    public static Guid GetCurrentTenantId(ClaimsPrincipal user)
    {
        return Guid.Parse(user.FindFirst("TenantId")!.Value);
    }

    public static string GetCurrentRole(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    }

    public static string GetCurrentUserName(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Name)?.Value ?? user.FindFirst("UserName")?.Value ?? "Unknown";
    }

    public static bool IsSuperAdmin(ClaimsPrincipal user)
    {
        return GetCurrentRole(user) == "SuperAdmin";
    }
}
