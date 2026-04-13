using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Prestige.Api.Data;
using Prestige.Api.Domain;
using Prestige.Api.Endpoints.Consent.RequestResponse;
using Prestige.Api.Logging;

namespace Prestige.Api.Endpoints.Consent
{
    public class UserConsentServices : BaseService
    {
        private string? UserAuthId => Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        public UserConsentServices(
            PrestigeContext db,
            ILogger<UserConsentServices> logger,
            ClaimsPrincipal principal,
            IConfiguration config) : base(db, logger, principal, config)
        {
        }

        public async Task<UserConsentResponse> GetUserConsentAsync(string userId)
        {
            await EnsureAuthorizedAsync(userId);
            var consent = await GetOrCreateConsentAsync(userId);
            return new UserConsentResponse(consent);
        }

        public async Task<UserConsentResponse> UpdateUserConsentAsync(string userId, UpdateUserConsentRequest request)
        {
            await EnsureAuthorizedAsync(userId);
            var consent = await GetOrCreateConsentAsync(userId);

            consent.UpdateResearchPreferences(
                request.AllowsAnonymizedAggregation,
                request.AllowsModelTraining,
                request.TriggeredBy ?? "settings_toggle",
                request.ConsentVersion ?? GetConsentVersion());

            await PrestigeDb.SaveChangesAsync();
            return new UserConsentResponse(consent);
        }

        private async Task EnsureAuthorizedAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(UserAuthId) || userId != UserAuthId.Split("|").Last())
            {
                throw Logger.UserUnauthorized(userId);
            }

            var userExists = await PrestigeDb.Users.AnyAsync(user => user.Id == userId);
            if (!userExists)
            {
                throw Logger.UserNotFound(userId);
            }
        }

        private async Task<UserConsent> GetOrCreateConsentAsync(string userId)
        {
            var consent = await PrestigeDb.UserConsents
                .Include(current => current.History)
                .FirstOrDefaultAsync(current => current.UserId == userId);

            if (consent != null)
            {
                return consent;
            }

            consent = new UserConsent(userId, GetConsentVersion());
            PrestigeDb.UserConsents.Add(consent);
            await PrestigeDb.SaveChangesAsync();
            return consent;
        }

        private string GetConsentVersion()
        {
            return Config["Consent:Version"] ?? "v1";
        }
    }
}
