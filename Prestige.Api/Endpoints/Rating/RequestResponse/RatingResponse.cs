namespace Prestige.Api.Endpoints.Rating.RequestResponse
{
    public class RatingResponse
    {
        public string ItemId { get; set; }
        public string ItemType { get; set; }
        public int? CategoryId { get; set; }
        public decimal? PersonalScore { get; set; }
        public int? Position { get; set; }
        public int? RankWithinAlbum { get; set; } // For tracks only - positional rank within album
        public string? AlbumId { get; set; }
        public bool IsNewRating { get; set; }
    }
}