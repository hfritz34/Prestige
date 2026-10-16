using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prestige.Api.Data;

namespace Prestige.Api.Tests;

public sealed class CatalogAccountTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new($"Data Source=account-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
    private WebApplication app = null!;

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        app = Program.CreateApplication([], builder =>
        {
            builder.Environment.EnvironmentName = "Testing";
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Sqlite",
                ["ConnectionStrings:Sqlite"] = connection.ConnectionString,
                ["Auth0:Domain"] = "unused.invalid",
                ["Auth0:Audience"] = "catalog-tests"
                // No Spotify, Auth0 management, Azure or background-job credentials.
            });
            builder.WebHost.UseTestServer();
            builder.Services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
            builder.Services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
                options.DefaultForbidScheme = "Test";
            });
        });
        await app.StartAsync();
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<PrestigeContext>().Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await app.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task AccountBootstrapIsStableAndNeedsNoSpotifyCredentials()
    {
        var client = Client("auth0|new-listener");
        var first = await Me(client);
        var second = await Me(client);
        var id = first.GetProperty("id").GetString()!;
        Assert.True(Guid.TryParseExact(id, "N", out _));
        Assert.Equal(id, second.GetProperty("id").GetString());
        Assert.False(first.GetProperty("isSetup").GetBoolean());
        Assert.False(first.TryGetProperty("accessToken", out _));

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PrestigeContext>();
        var user = await db.Users.SingleAsync();
        Assert.Empty(user.AccessToken);
        Assert.Empty(user.RefreshToken);
        Assert.Single(await db.UserIdentities.ToListAsync());
        Assert.Single(await db.UserConsents.ToListAsync());
        Assert.Equal(4, await db.ConsentEvents.CountAsync());
    }

    [Fact]
    public async Task FullSubjectAndIssuerDetermineIdentity()
    {
        var email = await Me(Client("auth0|same-suffix"));
        var spotify = await Me(Client("spotify|same-suffix"));
        var otherIssuer = await Me(Client("auth0|same-suffix", "https://other.example.com/"));
        var ids = new[] { email, spotify, otherIssuer }.Select(user => user.GetProperty("id").GetString());
        Assert.Equal(3, ids.Distinct().Count());
    }

    [Fact]
    public async Task AnonymousAndMissingSubjectCannotCreateAccounts()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await app.GetTestClient().GetAsync("/users/me")).StatusCode);
        var missing = Client("<missing>");
        Assert.Equal(HttpStatusCode.Forbidden, (await missing.GetAsync("/users/me")).StatusCode);
        using var scope = app.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<PrestigeContext>().Users.ToListAsync());
    }

    [Fact]
    public async Task SuppliedAccountClaimAndRequestIdCannotImpersonateAnotherUser()
    {
        var victim = await Me(Client("auth0|victim"));
        var victimId = victim.GetProperty("id").GetString()!;
        var attacker = Client("auth0|attacker");
        attacker.DefaultRequestHeaders.Add("X-Test-Account", victimId);
        var own = await Me(attacker);
        var ownId = own.GetProperty("id").GetString()!;
        Assert.NotEqual(victimId, ownId);
        Assert.Equal(HttpStatusCode.Unauthorized, (await attacker.GetAsync($"/users/{victimId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await attacker.PostAsJsonAsync("/users", new { id = victimId })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await attacker.PatchAsJsonAsync($"/users/{victimId}/nickname", new { nickName = "stolen" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await attacker.PatchAsJsonAsync($"/users/{ownId}/nickname", new { nickName = "listener" })).StatusCode);
        Assert.Equal("listener", (await Me(attacker)).GetProperty("nickName").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await attacker.GetAsync($"/users/{victimId}/tokens")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await attacker.GetAsync($"/users/{ownId}/tokens")).StatusCode);
    }

    private HttpClient Client(string subject, string issuer = "https://accounts.example.com/")
    {
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-Subject", subject);
        client.DefaultRequestHeaders.Add("X-Test-Issuer", issuer);
        return client;
    }

    private static async Task<JsonElement> Me(HttpClient client)
    {
        var response = await client.GetAsync("/users/me");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}

// Only registered in this test host. Production always validates signed JWTs.
public sealed class TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("X-Test-Subject")) return Task.FromResult(AuthenticateResult.NoResult());
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Request.Headers["X-Test-Subject"] == "<missing>" ? "" : Request.Headers["X-Test-Subject"].ToString()),
            new Claim("iss", Request.Headers["X-Test-Issuer"].ToString()),
            new Claim("prestige:account_id", Request.Headers["X-Test-Account"].ToString())
        };
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name)));
    }
}
