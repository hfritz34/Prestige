using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Prestige.Functions
{
    public class SpotifyImportOptionsFunction
    {
        private readonly ILogger _logger;

        public SpotifyImportOptionsFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SpotifyImportOptionsFunction>();
        }

        [Function("SpotifyImportOptions")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "options", Route = "import-options")] HttpRequestData req)
        {
            // Handle CORS preflight requests
            if (req.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                var corsResponse = req.CreateResponse(HttpStatusCode.OK);
                corsResponse.Headers.Add("Access-Control-Allow-Origin", "*");
                corsResponse.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
                corsResponse.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
                corsResponse.Headers.Add("Access-Control-Max-Age", "3600");
                return corsResponse;
            }

            _logger.LogInformation("Getting import options");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
            
            // Return available import endpoints and their capabilities
            var result = new
            {
                endpoints = new object[]
                {
                    new
                    {
                        name = "Streaming Import",
                        endpoint = "/api/spotifydataimportstreamingfunction",
                        description = "High-performance import with bulk operations and streaming",
                        features = new string[] { 
                            "Bulk Cosmos DB operations (10x faster)",
                            "Streaming JSON parsing (low memory usage)",
                            "Smart retry with exponential backoff",
                            "Duplicate file detection",
                            "Progress tracking via /api/import-progress/{batchId}",
                            "Automatic processing trigger",
                            "Handles 1M+ records efficiently"
                        },
                        recommended = true,
                        maxFileSize = "Unlimited",
                        batchProcessing = true,
                        performance = new
                        {
                            chunksSize = 10000,
                            parallelOperations = 50,
                            expectedThroughput = "5000-10000 records/second"
                        }
                    },
                    new
                    {
                        name = "Orchestrator Import",
                        endpoint = "/api/spotifydataimportorchestrator",
                        description = "Queue-based processing with Service Bus",
                        features = new string[] { "Async processing", "Scalable", "Progress tracking", "Fault tolerant" },
                        recommended = false,
                        maxFileSize = "Unlimited",
                        requiresServiceBus = true
                    },
                    new
                    {
                        name = "Direct Import",
                        endpoint = "/api/spotifydataimportfunction",
                        description = "Simple direct import (legacy)",
                        features = new string[] { "Simple", "Direct processing" },
                        recommended = false,
                        maxFileSize = "50MB",
                        batchProcessing = false
                    }
                },
                statusEndpoints = new object[]
                {
                    new
                    {
                        name = "Import Status",
                        endpoint = "/api/import-status/{batchId}",
                        description = "Get status of a specific import batch"
                    },
                    new
                    {
                        name = "User Import History",
                        endpoint = "/api/import-history/{userId}",
                        description = "Get import history for a user"
                    }
                }
            };
            
            await response.WriteAsJsonAsync(result);
            return response;
        }
    }
}