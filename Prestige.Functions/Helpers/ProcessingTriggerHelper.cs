using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Prestige.Functions.Helpers
{
    public class ProcessingTriggerHelper
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private readonly string _functionBaseUrl;

        public ProcessingTriggerHelper(ILogger logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _functionBaseUrl = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME") ?? "localhost:7071";
        }

        public async Task TriggerAutomaticProcessingAsync(string userId, int documentCount)
        {
            try
            {
                _logger.LogInformation($"Triggering automatic processing for {documentCount} documents for user {userId}");
                
                // Trigger the TriggerCosmosChangeFeeds function via HTTP
                var triggerUrl = $"https://{_functionBaseUrl}/api/TriggerCosmosChangeFeeds";
                
                var response = await _httpClient.PostAsync(triggerUrl, null);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully triggered automatic processing");
                }
                else
                {
                    _logger.LogWarning($"Failed to trigger automatic processing: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error triggering automatic processing: {ex.Message}");
                // Don't throw - we don't want to fail the import if triggering fails
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}