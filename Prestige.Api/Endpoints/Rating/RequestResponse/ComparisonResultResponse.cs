namespace Prestige.Api.Endpoints.Rating.RequestResponse
{
    public class ComparisonResultResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public decimal? UpdatedScore { get; set; }
        public int? UpdatedPosition { get; set; }
    }
}