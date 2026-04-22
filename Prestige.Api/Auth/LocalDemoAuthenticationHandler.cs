using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Prestige.Api.Auth
{
    public class LocalDemoAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "LocalDemo";

        public LocalDemoAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var config = Context.RequestServices.GetRequiredService<IConfiguration>();
            var demoUserId = config["LocalDemo:UserId"] ?? "demo-user";
            var auth0StyleId = demoUserId.Contains('|') ? demoUserId : $"auth0|{demoUserId}";

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, auth0StyleId),
                new Claim(ClaimTypes.Name, config["LocalDemo:Name"] ?? "Prestige Demo"),
                new Claim(ClaimTypes.Email, config["LocalDemo:Email"] ?? "demo@prestige.local")
            };

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
