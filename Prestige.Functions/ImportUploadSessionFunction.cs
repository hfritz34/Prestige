using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Prestige.Functions
{
    public class ImportUploadSessionFunction
    {
        private readonly ILogger _logger;
        private const int ChunkSize = 10000;

        public ImportUploadSessionFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ImportUploadSessionFunction>();
        }

        [Function("CreateImportUploadSession")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options", Route = "import-upload-session/{userId}")]
            HttpRequestData req,
            string userId)
        {
            if (req.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                var optionsResponse = req.CreateResponse(HttpStatusCode.OK);
                AddCorsHeaders(optionsResponse);
                return optionsResponse;
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                AddCorsHeaders(badRequest);
                await badRequest.WriteStringAsync("A userId route parameter is required.");
                return badRequest;
            }

            var sessionId = Guid.NewGuid().ToString();
            var hostRoot = $"{req.Url.Scheme}://{req.Url.Host}{(req.Url.IsDefaultPort ? string.Empty : $":{req.Url.Port}")}";
            var uploadUrl = $"{hostRoot}/api/spotifydataimportstreamingfunction?userId={Uri.EscapeDataString(userId)}&sessionId={Uri.EscapeDataString(sessionId)}";

            _logger.LogInformation("Created import upload session {SessionId} for user {UserId}", sessionId, userId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            AddCorsHeaders(response);
            await response.WriteAsJsonAsync(new
            {
                sessionId = sessionId,
                chunkSize = ChunkSize,
                uploadUrl = uploadUrl
            });
            return response;
        }

        private static void AddCorsHeaders(HttpResponseData response)
        {
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "POST, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, x-functions-key");
            response.Headers.Add("Access-Control-Max-Age", "3600");
        }
    }
}
