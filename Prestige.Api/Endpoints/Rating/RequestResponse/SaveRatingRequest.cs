namespace Prestige.Api.Endpoints.Rating.RequestResponse
{
    public class SaveRatingRequest
    {
        public string ItemId { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty;
        public decimal PersonalScore { get; set; }
        public int CategoryId { get; set; }
    }
}