using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Prestige.Api.Data;

namespace Prestige.Api.Services
{
    public class RecentlyPlayedCosmosService
    {
        private readonly CosmosClient? _cosmosClient;
        private readonly ILogger<RecentlyPlayedCosmosService> _logger;
        private readonly Container? _container;

        public RecentlyPlayedCosmosService(CosmosClient? cosmosClient, ILogger<RecentlyPlayedCosmosService> logger)
        {
            _cosmosClient = cosmosClient;
            _logger = logger;
            
            if (_cosmosClient != null)
            {
                var database = _cosmosClient.GetDatabase("MusicDB");
                _container = database.GetContainer("RecentlyPlayed");
            }
        }

        public async Task<List<RecentlyPlayedTrackDocument>> GetRecentlyPlayedTracksAsync(string userId, DateTime since)
        {
            if (_container == null)
            {
                _logger.LogWarning("CosmosDB not configured - returning empty recently played tracks");
                return new List<RecentlyPlayedTrackDocument>();
            }

            try
            {
                var queryDefinition = new QueryDefinition(
                    "SELECT c.trackId, c.userId, c.duration_ms, c.played_at FROM c WHERE c.userId = @userId AND c.played_at >= @since"
                )
                .WithParameter("@userId", userId)
                .WithParameter("@since", since.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

                var queryIterator = _container.GetItemQueryIterator<RecentlyPlayedTrackDocument>(
                    queryDefinition,
                    requestOptions: new QueryRequestOptions 
                    { 
                        PartitionKey = new PartitionKey(userId),
                        MaxItemCount = 100
                    });

                var results = new List<RecentlyPlayedTrackDocument>();

                while (queryIterator.HasMoreResults)
                {
                    var response = await queryIterator.ReadNextAsync();
                    results.AddRange(response);
                    
                    _logger.LogDebug($"Retrieved {response.Count} recently played tracks for user {userId} since {since}");
                }

                _logger.LogInformation($"Retrieved total of {results.Count} recently played tracks for user {userId} since {since}");
                return results.Distinct().ToList(); // Remove any duplicates
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error querying CosmosDB for recently played tracks for user {userId} since {since}");
                return new List<RecentlyPlayedTrackDocument>();
            }
        }
    }

    public class RecentlyPlayedTrackDocument
    {
        public string TrackId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public int Duration_ms { get; set; }
        public string Played_at { get; set; } = string.Empty;
    }
}