using System.Security.Claims;
using Prestige.Api.Configuration;

namespace Prestige.Api.Auth;

public static class CurrentAccount
{
    internal const string ClaimType = "prestige:account_id";
    internal const string AuthenticationType = "Prestige.Account";

    public static string? GetUserId(ClaimsPrincipal principal, IConfiguration configuration)
    {
        if (principal.Identity?.IsAuthenticated != true) return null;
        if (!RuntimeConfiguration.IsCatalog(configuration) || RuntimeConfiguration.IsLocalDemo(configuration))
            return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value.Split('|').Last();

        // Only the server-created identity can supply the application account ID.
        return principal.Identities.FirstOrDefault(identity => identity.AuthenticationType == AuthenticationType)
            ?.FindFirst(ClaimType)?.Value;
    }
}
