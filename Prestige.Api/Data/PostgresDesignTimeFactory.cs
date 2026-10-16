using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Prestige.Api.Data;

public sealed class PostgresDesignTimeFactory : IDesignTimeDbContextFactory<PostgresPrestigeContext>
{
    public PostgresPrestigeContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres");
        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new InvalidOperationException(
                "Set ConnectionStrings__Postgres before running PostgreSQL migration commands.");
        }

        return new PostgresPrestigeContext(new DbContextOptionsBuilder<PostgresPrestigeContext>()
            .UseNpgsql(connection, options => options.SetPostgresVersion(15, 0))
            .Options);
    }
}
