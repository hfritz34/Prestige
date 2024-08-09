using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public static class SpotifyDataImportFunction
{
    private static readonly string ConnectionString = Environment.GetEnvironmentVariable("CosmosDBConnectionString");
    private static readonly string DatabaseId = "MusicDB";
    private static readonly string ContainerId = "RecentlyPlayed";

    private static readonly CosmosClient cosmosClient = new CosmosClient(ConnectionString);
    private static readonly Container container = cosmosClient.GetContainer(DatabaseId, ContainerId);

    [FunctionName("SpotifyDataImportFunction")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request.");

        try
        {
            var formdata = await req.ReadFormAsync();
            var files = formdata.Files;

            foreach (var file in files)
            {
                using (var stream = file.OpenReadStream())
                using (var reader = new StreamReader(stream))
                {
                    var data = await reader.ReadToEndAsync();
                    var items = JsonConvert.DeserializeObject<List<SpotifyHistory>>(data);

                    foreach (var item in items)
                    {
                        var document = new Document
                        {
                            id = Guid.NewGuid().ToString(),
                            batchId = Guid.NewGuid().ToString(),
                            userId = item.Username,
                            trackId = item.SpotifyTrackUri,
                            duration_ms = item.MsPlayed,
                            played_at = item.Ts
                        };

                        await container.UpsertItemAsync(document);
                    }
                }
            }

            return new OkObjectResult("Data imported successfully.");
        }
        catch (Exception ex)
        {
            log.LogError($"Exception encountered: {ex.Message}");
            return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
        }
    }

    public class SpotifyHistory
    {
        public string Ts { get; set; }
        public string Username { get; set; }
        public string Platform { get; set; }
        public int MsPlayed { get; set; }
        public string ConnCountry { get; set; }
        public string IpAddrDecrypted { get; set; }
        public string UserAgentDecrypted { get; set; }
        public string MasterMetadataTrackName { get; set; }
        public string MasterMetadataAlbumArtistName { get; set; }
        public string MasterMetadataAlbumAlbumName { get; set; }
        public string SpotifyTrackUri { get; set; }
        public string EpisodeName { get; set; }
        public string EpisodeShowName { get; set; }
        public string SpotifyEpisodeUri { get; set; }
        public string ReasonStart { get; set; }
        public string ReasonEnd { get; set; }
        public bool Shuffle { get; set; }
        public bool? Skipped { get; set; }
        public bool Offline { get; set; }
        public long OfflineTimestamp { get; set; }
        public bool IncognitoMode { get; set; }
    }

    public class Document
    {
        public string id { get; set; }
        public string batchId { get; set; }
        public string userId { get; set; }
        public string trackId { get; set; }
        public int duration_ms { get; set; }
        public string played_at { get; set; }
    }
}
