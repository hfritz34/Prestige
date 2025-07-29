using Prestige.Api.Domain;

namespace Prestige.Api.Endpoints.Spotify.RequestResponse
{
    public class CurrentlyPlayingResponse
    {
        public TrackResponse Track { get; set; }
        public bool IsPlaying { get; set; }
        public int ProgressMs { get; set; }
        public int DurationMs { get; set; }
        
        public CurrentlyPlayingResponse(TrackResponse track, bool isPlaying, int progressMs, int durationMs)
        {
            Track = track;
            IsPlaying = isPlaying;
            ProgressMs = progressMs;
            DurationMs = durationMs;
        }
    }
}