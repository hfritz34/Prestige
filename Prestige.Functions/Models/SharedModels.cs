using Newtonsoft.Json;

namespace Prestige.Functions.Models
{
    public class Document
    {
        public string id { get; set; }
        public string batchId { get; set; }
        public string userId { get; set; }
        public string trackId { get; set; }
        public int duration_ms { get; set; }
        public string played_at { get; set; }
        public bool processed { get; set; } = false;
        public string lastTriggered { get; set; }
    }

    public class SpotifyHistory
    {
        [JsonProperty("ts")]
        public string Ts { get; set; }
        
        [JsonProperty("platform")]
        public string Platform { get; set; }
        
        [JsonProperty("ms_played")]
        public int MsPlayed { get; set; }
        
        [JsonProperty("conn_country")]
        public string ConnCountry { get; set; }
        
        [JsonProperty("ip_addr")]
        public string IpAddr { get; set; }
        
        [JsonProperty("master_metadata_track_name")]
        public string MasterMetadataTrackName { get; set; }
        
        [JsonProperty("master_metadata_album_artist_name")]
        public string MasterMetadataAlbumArtistName { get; set; }
        
        [JsonProperty("master_metadata_album_album_name")]
        public string MasterMetadataAlbumAlbumName { get; set; }
        
        [JsonProperty("spotify_track_uri")]
        public string SpotifyTrackUri { get; set; }
        
        [JsonProperty("episode_name")]
        public string EpisodeName { get; set; }
        
        [JsonProperty("episode_show_name")]
        public string EpisodeShowName { get; set; }
        
        [JsonProperty("spotify_episode_uri")]
        public string SpotifyEpisodeUri { get; set; }
        
        [JsonProperty("audiobook_title")]
        public string AudiobookTitle { get; set; }
        
        [JsonProperty("audiobook_uri")]
        public string AudiobookUri { get; set; }
        
        [JsonProperty("audiobook_chapter_uri")]
        public string AudiobookChapterUri { get; set; }
        
        [JsonProperty("audiobook_chapter_title")]
        public string AudiobookChapterTitle { get; set; }
        
        [JsonProperty("reason_start")]
        public string ReasonStart { get; set; }
        
        [JsonProperty("reason_end")]
        public string ReasonEnd { get; set; }
        
        [JsonProperty("shuffle")]
        public bool Shuffle { get; set; }
        
        [JsonProperty("skipped")]
        public bool? Skipped { get; set; }
        
        [JsonProperty("offline")]
        public bool Offline { get; set; }
        
        [JsonProperty("offline_timestamp")]
        public long? OfflineTimestamp { get; set; }
        
        [JsonProperty("incognito_mode")]
        public bool IncognitoMode { get; set; }
    }
}