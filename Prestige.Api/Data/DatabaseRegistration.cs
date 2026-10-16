using Microsoft.EntityFrameworkCore;
using Prestige.Api.Configuration;

namespace Prestige.Api.Data;

public static class DatabaseRegistration
{
    public static void AddPrestigeDatabase(this IServiceCollection services,
        IConfiguration configuration, IHostEnvironment environment)
    {
        var demo = RuntimeConfiguration.IsLocalDemo(configuration);
        var provider = demo ? "Sqlite" : configuration["Database:Provider"] ??
            (RuntimeConfiguration.IsCatalog(configuration) ? "Postgres" : "SqlServer");

        switch (provider.ToLowerInvariant())
        {
            case "postgres":
                var postgres = configuration.GetConnectionString("Postgres");
                if (string.IsNullOrWhiteSpace(postgres))
                    throw new InvalidOperationException("Set ConnectionStrings:Postgres for the PostgreSQL database.");

                services.AddDbContextPool<PostgresPrestigeContext>(options => options
                    .UseNpgsql(postgres, pg => pg.SetPostgresVersion(15, 0).CommandTimeout(30)), poolSize: 16);
                services.AddScoped<PrestigeContext>(sp => sp.GetRequiredService<PostgresPrestigeContext>());
                break;

            case "sqlite":
                if (!environment.IsDevelopment() && !environment.IsEnvironment("LocalDemo") &&
                    !environment.IsEnvironment("Testing"))
                    throw new InvalidOperationException("SQLite is reserved for local development and tests. Use PostgreSQL for catalog deployments.");

                var sqlite = demo
                    ? $"Data Source={configuration["LocalDemo:DatabasePath"] ?? "prestige_demo.db"}"
                    : configuration.GetConnectionString("Sqlite") ?? "Data Source=prestige_dev.db";
                services.AddDbContextPool<PrestigeContext>(options => options.UseSqlite(sqlite), poolSize: 16);
                break;

            case "sqlserver":
                if (RuntimeConfiguration.IsCatalog(configuration))
                    throw new InvalidOperationException("Catalog mode uses PostgreSQL. SQL Server is available only in Legacy mode.");

                var sql = configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING") ??
                    configuration["AZURE_SQL_CONNECTIONSTRING"];
                if (string.IsNullOrWhiteSpace(sql))
                    throw new InvalidOperationException("Set ConnectionStrings:AZURE_SQL_CONNECTIONSTRING for the legacy database.");

                services.AddDbContextPool<PrestigeContext>(options => options.UseSqlServer(sql,
                    server => server.EnableRetryOnFailure(3).CommandTimeout(30)), poolSize: 16);
                break;

            default:
                throw new InvalidOperationException("Database:Provider must be Postgres, Sqlite, or SqlServer.");
        }
    }
}
