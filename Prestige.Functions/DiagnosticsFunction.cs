using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Prestige.Functions
{
    public class DiagnosticsFunction
    {
        private readonly ILogger _logger;

        public DiagnosticsFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DiagnosticsFunction>();
        }

        [Function("Diagnostics")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "diagnostics")] HttpRequestData req)
        {
            _logger.LogInformation("Diagnostics endpoint called");

            var diagnostics = new
            {
                Status = "Running",
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                Environment = new
                {
                    MachineName = Environment.MachineName,
                    OSVersion = Environment.OSVersion.ToString(),
                    ProcessorCount = Environment.ProcessorCount,
                    Is64BitProcess = Environment.Is64BitOperatingSystem,
                    RuntimeVersion = RuntimeInformation.FrameworkDescription,
                    FunctionsVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                },
                Configuration = new
                {
                    FunctionsWorkerRuntime = Environment.GetEnvironmentVariable("FUNCTIONS_WORKER_RUNTIME") ?? "Not Set",
                    FunctionsExtensionVersion = Environment.GetEnvironmentVariable("FUNCTIONS_EXTENSION_VERSION") ?? "Not Set",
                    WebsiteRunFromPackage = Environment.GetEnvironmentVariable("WEBSITE_RUN_FROM_PACKAGE") ?? "Not Set",
                    WebsiteTimeZone = Environment.GetEnvironmentVariable("WEBSITE_TIME_ZONE") ?? "Not Set",
                    WebsiteInstanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") ?? "Local",
                    WebsiteSiteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") ?? "Local"
                },
                ConnectionsStatus = new
                {
                    CosmosDB = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CosmosDBConnectionString")),
                    SqlServer = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SqlConnectionString")),
                    BlobStorage = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AzureWebJobsStorage")),
                    ServiceBus = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ServiceBusConnectionString")),
                    ApplicationInsights = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING"))
                },
                Auth0Config = new
                {
                    Domain = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("Auth0Domain")),
                    ClientId = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("Auth0ClientId")),
                    ClientSecret = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("Auth0ClientSecret")),
                    Audience = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("Auth0Audience"))
                },
                SpotifyConfig = new
                {
                    ClientId = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SpotifyClientId")),
                    ClientSecret = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SpotifyClientSecret"))
                },
                Functions = new object[]
                {
                    new { Name = "HealthCheck", Type = "HttpTrigger", Route = "/api/HealthCheck" },
                    new { Name = "Diagnostics", Type = "HttpTrigger", Route = "/api/diagnostics" },
                    new { Name = "SpotifyDataImportStreamingFunction", Type = "HttpTrigger", Route = "/api/SpotifyDataImportStreamingFunction" },
                    new { Name = "SpotifyImportOptions", Type = "HttpTrigger", Route = "/api/import-options" },
                    new { Name = "SpotifyImportProgress", Type = "HttpTrigger", Route = "/api/import-progress/{batchId}" },
                    new { Name = "SpotifyImportStatus", Type = "HttpTrigger", Route = "/api/import-status/{batchId}" },
                    new { Name = "RecentlyPlayedTrigger", Type = "TimerTrigger", Schedule = "0 0 * * * * (Every hour)" },
                    new { Name = "CosmosDBParser", Type = "CosmosDBTrigger", Database = "MusicDB", Container = "RecentlyPlayed" },
                    new { Name = "ManualRecentlyPlayedTrigger", Type = "HttpTrigger", Route = "/api/trigger-recently-played" },
                    new { Name = "TriggerCosmosChangeFeeds", Type = "HttpTrigger", Route = "/api/trigger-cosmos-change-feeds" }
                },
                Recommendations = new[]
                {
                    "Ensure APPLICATIONINSIGHTS_CONNECTION_STRING is configured in Azure",
                    "Verify WEBSITE_RUN_FROM_PACKAGE is set to 1 in Azure",
                    "Check Azure Portal > Function App > Configuration for all settings",
                    "Use 'func azure functionapp publish <app-name>' to deploy",
                    "Check Function App > Functions in Azure Portal to see if functions are listed"
                }
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
            
            await response.WriteAsJsonAsync(diagnostics);
            return response;
        }

        [Function("ManualRecentlyPlayedTrigger")]
        public async Task<HttpResponseData> ManualTrigger([HttpTrigger(AuthorizationLevel.Function, "post", Route = "trigger-recently-played")] HttpRequestData req)
        {
            _logger.LogInformation("Manual trigger for RecentlyPlayedTrigger initiated");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            
            await response.WriteAsJsonAsync(new
            {
                Status = "Manual trigger endpoint created",
                Message = "Use Azure Portal to manually trigger the RecentlyPlayedTrigger timer function",
                ExecutedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                Instructions = "Go to Azure Portal > Function Apps > Functions > RecentlyPlayedTrigger > Test/Run"
            });
            
            return response;
        }
    }
}