using System.Net;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Prestige.Api.Configuration;
using Prestige.Api.Data;

namespace Prestige.Api.Tests;

public class CatalogRuntimeTests
{
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Testing")]
    public void DemoAuthenticationCannotBeEnabledOutsideLocalEnvironments(string environment)
    {
        var configuration = Config(new() { ["LocalDemo:Enabled"] = "true" });
        var exception = Assert.Throws<InvalidOperationException>(() =>
            RuntimeConfiguration.Validate(configuration, new TestEnvironment(environment)));
        Assert.Contains("Local demo authentication", exception.Message);
    }

    [Fact]
    public void LegacyDemoEnvironmentVariableIsAlsoGuarded()
    {
        Assert.Throws<InvalidOperationException>(() => RuntimeConfiguration.Validate(
            Config(new() { ["PRESTIGE_LOCAL_DEMO"] = "true" }), new TestEnvironment("Production")));
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("LocalDemo")]
    public void LocalDemoRemainsAvailable(string environment)
    {
        RuntimeConfiguration.Validate(Config(new() { ["LocalDemo:Enabled"] = "true" }),
            new TestEnvironment(environment));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("*")]
    [InlineData("http://example.com")]
    [InlineData("https://example.com/")]
    [InlineData("https://example.com/path")]
    public void ProductionRejectsMissingOrInvalidWebOrigins(string? origin)
    {
        var configuration = Config(new()
        {
            ["Auth0:Domain"] = "example.auth0.com",
            ["Auth0:Audience"] = "https://api.example.com",
            ["Cors:AllowedOrigins:0"] = origin
        });
        Assert.Throws<InvalidOperationException>(() =>
            RuntimeConfiguration.Validate(configuration, new TestEnvironment("Production")));
    }

    [Fact]
    public void CatalogHasNoSilentFallbackToLegacyDatabaseOrInfrastructure()
    {
        var configuration = Config(new()
        {
            ["ConnectionStrings:AZURE_SQL_CONNECTIONSTRING"] = "Server=legacy.invalid",
            ["ConnectionStrings:Redis"] = "legacy.invalid"
        });
        Assert.True(RuntimeConfiguration.IsCatalog(configuration));
        Assert.False(RuntimeConfiguration.IsHangfireEnabled(configuration));
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddPrestigeDatabase(configuration, new TestEnvironment("Production")));
        Assert.Contains("ConnectionStrings:Postgres", exception.Message);
    }

    [Fact]
    public async Task CatalogStartsWithoutCloudDependenciesAndReadinessDetectsMissingSchema()
    {
        // One open SQLite connection keeps the test database alive across scoped contexts.
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection(
            $"Data Source=runtime-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        await using var app = Program.CreateApplication([], builder =>
        {
            builder.Environment.EnvironmentName = "Testing";
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Sqlite",
                ["ConnectionStrings:Sqlite"] = connection.ConnectionString,
                ["ConnectionStrings:Redis"] = "unused.invalid",
                ["ConnectionStrings:CosmosDB"] = "not-a-connection-string",
                ["Cors:AllowedOrigins:0"] = "https://music.example.com",
                ["Auth0:Domain"] = "example.auth0.com",
                ["Auth0:Audience"] = "https://api.example.com"
            });
            builder.WebHost.UseTestServer();
        });
        await app.StartAsync();

        Assert.Null(app.Services.GetService<IBackgroundJobClient>());
        Assert.Null(app.Services.GetService<CosmosClient>());
        Assert.IsType<MemoryDistributedCache>(app.Services.GetRequiredService<IDistributedCache>());

        var client = app.GetTestClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/health")).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/api/ready")).StatusCode);

        using (var scope = app.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<PrestigeContext>().Database.EnsureCreatedAsync();
        }
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/ready")).StatusCode);

        // Production endpoints still require authentication when the optional services are disabled.
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("api/rating/categories")).StatusCode);
        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/rating/categories");
        preflight.Headers.Add("Origin", "https://music.example.com");
        preflight.Headers.Add("Access-Control-Request-Method", "GET");
        var response = await client.SendAsync(preflight);
        Assert.Equal("https://music.example.com", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    private static IConfiguration Config(Dictionary<string, string?> settings) =>
        new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

    private sealed class TestEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Prestige.Api.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
