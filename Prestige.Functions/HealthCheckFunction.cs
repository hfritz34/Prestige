using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Prestige.Functions
{
    public class HealthCheckFunction
    {
        private readonly ILogger _logger;

        public HealthCheckFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HealthCheckFunction>();
        }

        [Function("HealthCheck")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            _logger.LogInformation("Health check endpoint called");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            var healthStatus = new
            {
                Status = "Healthy",
                Functions = new[]
                {
                    "SpotifyDataImportFunction - Original synchronous import",
                    "SpotifyDataImportStreamingFunction - Streaming with blob storage",
                    "SpotifyDataImportOrchestrator - Queue-based orchestrator (requires Service Bus)",
                    "SpotifyDataImportProcessor - Queue processor (requires Service Bus)",
                    "SpotifyImportStatusFunction - Import status checking"
                },
                Configuration = new
                {
                    CosmosDB = !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("CosmosDBConnectionString")),
                    SqlServer = !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("SqlConnectionString")),
                    BlobStorage = !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("AzureWebJobsStorage")),
                    ServiceBus = !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("ServiceBusConnectionString"))
                }
            };

            response.WriteString(System.Text.Json.JsonSerializer.Serialize(healthStatus));
            return response;
        }
    }
}