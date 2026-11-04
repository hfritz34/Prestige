using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Prestige.Api.Auth;
using Prestige.Api.Domain;
using Prestige.Api.Endpoints.Catalog;

namespace Prestige.Api.Tests;

public sealed partial class PostgresDatabaseTests
{
    [PostgresFact]
    public async Task ConcurrentFriendRequestsAndAcceptsStayMutualAndIdempotent()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        var configuration = new ConfigurationBuilder().Build();
        async Task<ClaimsPrincipal> Account(string subject) => await new CatalogAccountTransformation(db, configuration)
            .TransformAsync(new ClaimsPrincipal(new ClaimsIdentity(
                [new(ClaimTypes.NameIdentifier, subject), new("iss", "https://accounts.example.com/")], "Test")));
        var alice = await Account("auth0|alice");
        var bob = await Account("auth0|bob");
        var aliceId = CurrentAccount.GetUserId(alice, configuration)!;
        var bobId = CurrentAccount.GetUserId(bob, configuration)!;
        async Task<CatalogFriendResponse> Change(ClaimsPrincipal principal, string id, string action)
        {
            await using var requestDb = CreateContext();
            return await new CatalogFriendsServices(requestDb, principal, configuration).ChangeAsync(id, action);
        }
        var sent = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Change(alice, bobId, "send")));
        Assert.Single(sent.Select(s => s.RequestDate).Distinct());
        Assert.Single(await db.Friendships.ToListAsync());
        var accepted = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Change(bob, aliceId, "accept")));
        Assert.Single(accepted.Select(s => s.AcceptedDate).Distinct());
        db.ChangeTracker.Clear();
        Assert.Equal(2, await db.Friendships.CountAsync(f => f.Status == FriendRequestStatus.Accepted));
        await Task.WhenAll(Change(alice, bobId, "remove"), Change(bob, aliceId, "remove"));
        Assert.Empty(await db.Friendships.ToListAsync());
        // Crossed requests must not silently accept each other or create two pending directions.
        var cross = await Task.WhenAll(new[] { (alice, bobId), (bob, aliceId) }.Select(async pair =>
        {
            try { await Change(pair.Item1, pair.Item2, "send"); return true; }
            catch (Api.Exceptions.ArgumentException) { return false; }
        }));
        Assert.Single(cross.Where(success => success));
        Assert.Equal(FriendRequestStatus.Pending, (await db.Friendships.AsNoTracking().SingleAsync()).Status);
        var list = await new CatalogFriendsServices(db, alice, configuration).ListAsync("outgoing", 0, 100);
        Assert.InRange(list.Count, 0, 1);
    }
}
