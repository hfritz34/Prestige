using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prestige.Api.Data;
using Prestige.Api.Domain;
using Prestige.Api.Endpoints.Catalog;

namespace Prestige.Api.Tests;

public sealed partial class CatalogAccountTests
{
    [Fact]
    public async Task FriendRequestsRequireRecipientConsentAndReturnTheOtherPerson()
    {
        var alice = Client("auth0|alice");
        var bob = Client("auth0|bob");
        var third = Client("auth0|third");
        var aliceId = (await Me(alice)).GetProperty("id").GetString()!;
        var bobId = (await Me(bob)).GetProperty("id").GetString()!;
        await Me(third);
        // Names that the legacy service treats as test accounts must not grant friendship.
        (await bob.PatchAsJsonAsync($"/users/{bobId}/nickname", new { nickName = "Test Buddy" })).EnsureSuccessStatusCode();
        var sent = await alice.PostAsJsonAsync($"/api/catalog/friends/requests/{bobId}", new { userId = bobId });
        sent.EnsureSuccessStatusCode();
        Assert.Equal("Pending", (await sent.Content.ReadFromJsonAsync<CatalogFriendResponse>())!.Status);
        Assert.Empty((await alice.GetFromJsonAsync<List<CatalogFriendResponse>>("/api/catalog/friends"))!);
        var incoming = Assert.Single((await bob.GetFromJsonAsync<List<CatalogFriendResponse>>("/api/catalog/friends/requests"))!);
        Assert.Equal(aliceId, incoming.Id);
        Assert.Equal(bobId, Assert.Single((await alice.GetFromJsonAsync<List<CatalogFriendResponse>>("/api/catalog/friends/requests?direction=outgoing"))!).Id);
        Assert.Empty((await third.GetFromJsonAsync<List<CatalogFriendResponse>>("/api/catalog/friends/requests"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await alice.PostAsJsonAsync($"/api/catalog/friends/requests/{bobId}/accept", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await third.PostAsJsonAsync($"/api/catalog/friends/requests/{aliceId}/accept", new { userId = bobId })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await bob.PostAsJsonAsync($"/api/catalog/friends/requests/{aliceId}", new { })).StatusCode);
        // Repeated sends/accepts are safe when an HTTP response was lost.
        (await alice.PostAsJsonAsync($"/api/catalog/friends/requests/{bobId}", new { })).EnsureSuccessStatusCode();
        for (var i = 0; i < 2; i++)
        {
            var accepted = await bob.PostAsJsonAsync($"/api/catalog/friends/requests/{aliceId}/accept", new { });
            accepted.EnsureSuccessStatusCode();
            Assert.Equal(aliceId, (await accepted.Content.ReadFromJsonAsync<CatalogFriendResponse>())!.Id);
        }
        Assert.Equal(bobId, Assert.Single((await alice.GetFromJsonAsync<List<CatalogFriendResponse>>("/api/catalog/friends"))!).Id);
        Assert.Equal(aliceId, Assert.Single((await bob.GetFromJsonAsync<List<CatalogFriendResponse>>("/api/catalog/friends"))!).Id);
        Assert.Empty((await bob.GetFromJsonAsync<List<CatalogFriendResponse>>("/api/catalog/friends/requests"))!);
        (await alice.DeleteAsync($"/api/catalog/friends/{bobId}")).EnsureSuccessStatusCode();
        (await alice.DeleteAsync($"/api/catalog/friends/{bobId}")).EnsureSuccessStatusCode();
        Assert.Empty((await bob.GetFromJsonAsync<List<CatalogFriendResponse>>("/api/catalog/friends"))!);
        using var scope = app.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<PrestigeContext>().Friendships.ToListAsync());
    }

    [Fact]
    public async Task DeclineCancelValidationAndLegacyRoutesCannotGrantFriendship()
    {
        var sender = Client("auth0|sender");
        var recipient = Client("auth0|recipient");
        var senderId = (await Me(sender)).GetProperty("id").GetString()!;
        var recipientId = (await Me(recipient)).GetProperty("id").GetString()!;
        Assert.Equal(HttpStatusCode.Unauthorized, (await app.GetTestClient().GetAsync("/api/catalog/friends")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await sender.PostAsJsonAsync($"/api/catalog/friends/requests/{senderId}", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await sender.PostAsJsonAsync("/api/catalog/friends/requests/unknown", new { })).StatusCode);
        foreach (var path in new[] { "?take=101", "?skip=-1", "/requests?direction=friends", "/requests?take=0" })
            Assert.Equal(HttpStatusCode.BadRequest, (await sender.GetAsync($"/api/catalog/friends{path}")).StatusCode);
        foreach (var path in new[] { $"{senderId}/friends/{recipientId}", $"{recipientId}/friend-requests/{senderId}/accept", $"{senderId}/friend-requests/{recipientId}" })
            Assert.Equal(HttpStatusCode.Conflict, (await sender.PostAsJsonAsync($"/api/Friendships/{path}", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await sender.GetAsync($"/api/Friendships/{recipientId}/friends")).StatusCode);
        (await sender.PostAsJsonAsync($"/api/catalog/friends/requests/{recipientId}", new { })).EnsureSuccessStatusCode();
        for (var i = 0; i < 2; i++)
            (await recipient.PostAsJsonAsync($"/api/catalog/friends/requests/{senderId}/decline", new { })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await recipient.PostAsJsonAsync($"/api/catalog/friends/requests/{senderId}/accept", new { })).StatusCode);
        Assert.Empty((await sender.GetFromJsonAsync<List<CatalogFriendResponse>>("/api/catalog/friends/requests?direction=outgoing"))!);
        (await sender.PostAsJsonAsync($"/api/catalog/friends/requests/{recipientId}", new { })).EnsureSuccessStatusCode();
        (await sender.DeleteAsync($"/api/catalog/friends/requests/{recipientId}")).EnsureSuccessStatusCode();
        Assert.Empty((await recipient.GetFromJsonAsync<List<CatalogFriendResponse>>("/api/catalog/friends/requests"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await recipient.PostAsJsonAsync($"/api/catalog/friends/requests/{senderId}/accept", new { })).StatusCode);
    }

    [Fact]
    public async Task FriendListsAreBoundedStableAndExposeOnlyPublicFields()
    {
        var viewer = Client("auth0|viewer");
        var viewerId = (await Me(viewer)).GetProperty("id").GetString()!;
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PrestigeContext>();
            for (var i = 0; i < 102; i++)
            {
                var id = $"friend-{i:000}";
                db.Users.Add(new User(id, "Fan", "fan", "private@example.com", "", "secret-access", "secret-refresh"));
                db.Friendships.Add(new Friendship { UserId = viewerId, FriendId = id, Status = FriendRequestStatus.Accepted,
                    RequestDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });
            }
            await db.SaveChangesAsync();
        }
        var first = (await viewer.GetFromJsonAsync<List<CatalogFriendResponse>>("/api/catalog/friends?take=100"))!;
        var second = (await viewer.GetFromJsonAsync<List<CatalogFriendResponse>>("/api/catalog/friends?skip=100&take=100"))!;
        Assert.Equal(100, first.Count);
        Assert.Equal(new[] { "friend-100", "friend-101" }, second.Select(f => f.Id));
        var json = await viewer.GetStringAsync("/api/catalog/friends");
        Assert.DoesNotContain("secret", json);
        Assert.DoesNotContain("private@example.com", json);
        Assert.DoesNotContain("accessToken", json);
        Assert.DoesNotContain("email", json);
    }
}
