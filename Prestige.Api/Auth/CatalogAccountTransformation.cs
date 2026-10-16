using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Prestige.Api.Configuration;
using Prestige.Api.Data;
using Prestige.Api.Domain;

namespace Prestige.Api.Auth;

public sealed class CatalogAccountTransformation(PrestigeContext db, IConfiguration configuration)
    : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true || !RuntimeConfiguration.IsCatalog(configuration) ||
            RuntimeConfiguration.IsLocalDemo(configuration)) return principal;

        // Rebuild our identity on every transformation; never trust an account ID supplied in a token.
        var result = new ClaimsPrincipal(principal.Identities
            .Where(identity => identity.AuthenticationType != CurrentAccount.AuthenticationType)
            .Select(identity => identity.Clone()));
        var issuer = result.FindFirst("iss")?.Value;
        var subject = result.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? result.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(issuer) || issuer.Length > 200 ||
            string.IsNullOrWhiteSpace(subject) || subject.Length > 255) return result;

        var userId = await FindAccountAsync(issuer, subject);
        if (userId == null)
        {
            var id = Guid.NewGuid().ToString("N");
            var user = new User(id, "Music fan", $"listener-{id[..10]}", "", "", "", "");
            db.UserIdentities.Add(new UserIdentity(issuer, subject, user));
            db.UserConsents.Add(new UserConsent(id, configuration["Consent:Version"] ?? "v1"));

            try
            {
                // Identity, account and consent are committed atomically by EF's transaction.
                await db.SaveChangesAsync();
                userId = id;
            }
            catch (DbUpdateException)
            {
                // Concurrent first requests may both provision. Only a persisted mapping wins;
                // unrelated database failures still propagate instead of granting access.
                db.ChangeTracker.Clear();
                userId = await FindAccountAsync(issuer, subject);
                if (userId == null) throw;
            }
        }

        result.AddIdentity(new ClaimsIdentity(
            [new Claim(CurrentAccount.ClaimType, userId)], CurrentAccount.AuthenticationType));
        return result;
    }

    private Task<string?> FindAccountAsync(string issuer, string subject) => db.UserIdentities
        .Where(identity => identity.Issuer == issuer && identity.Subject == subject)
        .Select(identity => identity.UserId).SingleOrDefaultAsync();
}
