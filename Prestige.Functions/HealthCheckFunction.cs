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
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            _logger.LogInformation("Health check endpoint called");

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

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
            
            await response.WriteAsJsonAsync(healthStatus);
            return response;
        }
    }
}