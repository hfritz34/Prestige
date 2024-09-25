using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;

public class SpotifyDataImportFunction
{
    private readonly ILogger _logger;
    private readonly Container _container;

    public SpotifyDataImportFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<SpotifyDataImportFunction>();

        string connectionString = Environment.GetEnvironmentVariable("CosmosDBConnectionString");
        string databaseId = "MusicDB";
        string containerId = "RecentlyPlayed";

        var cosmosClient = new CosmosClient(connectionString);
        _container = cosmosClient.GetContainer(databaseId, containerId);
    }

    [Function("SpotifyDataImportFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        try
        {
            // Check Content-Type header
            if (!req.Headers.TryGetValues("Content-Type", out var contentTypeValues))
            {
                var responseError = req.CreateResponse(HttpStatusCode.BadRequest);
                await responseError.WriteStringAsync("Missing Content-Type header.");
                return responseError;
            }

            var contentType = string.Join("; ", contentTypeValues);
            var mediaTypeHeader = MediaTypeHeaderValue.Parse(contentType);

            if (!mediaTypeHeader.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                var responseError = req.CreateResponse(HttpStatusCode.BadRequest);
                await responseError.WriteStringAsync("Invalid Content-Type header. Expected 'multipart/form-data'.");
                return responseError;
            }

            // Get boundary
            var boundary = HeaderUtilities.RemoveQuotes(mediaTypeHeader.Boundary).Value;
            if (string.IsNullOrEmpty(boundary))
            {
                var responseError = req.CreateResponse(HttpStatusCode.BadRequest);
                await responseError.WriteStringAsync("Could not determine boundary from Content-Type header.");
                return responseError;
            }

            // Read the multipart form data
            var reader = new MultipartReader(boundary, req.Body);
            MultipartSection section;
            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);
                if (!hasContentDispositionHeader)
                    continue;

                if (contentDisposition.DispositionType.Equals("form-data") && contentDisposition.FileName.HasValue)
                {
                    var fileName = contentDisposition.FileName.Value;

                    using (var stream = section.Body)
                    using (var readerStream = new StreamReader(stream))
                    {
                        var data = await readerStream.ReadToEndAsync();
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

                            await _container.UpsertItemAsync(document);
                        }
                    }
                }
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync("Data imported successfully.");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception encountered: {ex.Message}");

            var responseError = req.CreateResponse(HttpStatusCode.InternalServerError);
            await responseError.WriteStringAsync("An error occurred while processing your request.");
            return responseError;
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
