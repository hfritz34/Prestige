using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prestige.Api.Data;

namespace Prestige.Api.Services;

public sealed class DatabaseReadinessCheck(PrestigeContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Also detect a reachable but uninitialized database; connectivity alone is insufficient.
            var hasCategories = await db.RatingCategories.AsNoTracking().AnyAsync(cancellationToken);
            return hasCategories
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Database has not been initialized.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Database is unavailable or has not been initialized.");
        }
    }
}
