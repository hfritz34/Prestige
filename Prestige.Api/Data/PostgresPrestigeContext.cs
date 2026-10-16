using Microsoft.EntityFrameworkCore;

namespace Prestige.Api.Data;

/// <summary>
/// Shares the application model, but owns a fresh PostgreSQL migration history.
/// SQL Server migrations continue to belong to PrestigeContext.
/// </summary>
public sealed class PostgresPrestigeContext(DbContextOptions<PostgresPrestigeContext> options)
    : PrestigeContext(options);
