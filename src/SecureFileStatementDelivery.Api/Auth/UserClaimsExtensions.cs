using System.Security.Claims;

namespace SecureFileStatementDelivery.Api.Auth;

internal static class UserClaimsExtensions
{
    public static string? GetCustomerId(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue("customerId")
            ?? principal.FindFirstValue("cid");
    }

    public static string GetActor(this ClaimsPrincipal principal)
    {
        return principal.Identity?.Name
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "unknown";
    }
}
