namespace Prestige.Api.Configuration;

/// <summary>Catalog deployments have no recurring collection infrastructure.</summary>
public static class RuntimeConfiguration
{
    public static bool IsLocalDemo(IConfiguration configuration) =>
        configuration.GetValue<bool>("LocalDemo:Enabled") ||
        configuration.GetValue<bool>("PRESTIGE_LOCAL_DEMO");

    public static bool IsCatalog(IConfiguration configuration) =>
        !string.Equals(configuration["Prestige:Mode"], "Legacy", StringComparison.OrdinalIgnoreCase);

    public static bool IsHangfireEnabled(IConfiguration configuration) =>
        !IsCatalog(configuration) && !IsLocalDemo(configuration) &&
        !configuration.GetValue<bool>("DisableHangfire");

    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        var mode = configuration["Prestige:Mode"] ?? "Catalog";
        if (!mode.Equals("Catalog", StringComparison.OrdinalIgnoreCase) &&
            !mode.Equals("Legacy", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Prestige:Mode must be Catalog or Legacy.");
        }

        if (IsLocalDemo(configuration) &&
            !environment.IsDevelopment() && !environment.IsEnvironment("LocalDemo"))
        {
            throw new InvalidOperationException(
                "Local demo authentication is only allowed in Development or LocalDemo environments.");
        }

        if (environment.IsProduction())
        {
            if (string.IsNullOrWhiteSpace(configuration["Auth0:Domain"]) ||
                string.IsNullOrWhiteSpace(configuration["Auth0:Audience"]))
            {
                throw new InvalidOperationException("Production requires Auth0:Domain and Auth0:Audience.");
            }

            var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            if (origins.Length == 0 || origins.Any(origin =>
                    !Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
                    uri.Scheme != Uri.UriSchemeHttps ||
                    origin != uri.GetLeftPart(UriPartial.Authority)))
            {
                throw new InvalidOperationException(
                    "Production requires Cors:AllowedOrigins with explicit HTTPS origins (without paths or trailing slashes).");
            }
        }
    }
}
