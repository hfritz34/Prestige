using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Prestige.Api.Data;

/// <summary>Serialize catalog mutations for one account on PostgreSQL, inside their transaction.</summary>
public static class AccountWriteTransaction
{
    public static async Task<IDbContextTransaction> BeginAsync(PrestigeContext db, string userId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (db.Database.IsNpgsql())
            {
                await db.Users.FromSqlInterpolated($"SELECT * FROM \"User\" WHERE \"Id\" = {userId} FOR UPDATE")
                    .AsNoTracking().SingleAsync(cancellationToken);
            }
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }
}
