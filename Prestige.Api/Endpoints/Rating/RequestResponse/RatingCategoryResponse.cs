namespace Prestige.Api.Endpoints.Rating.RequestResponse
{
    public class RatingCategoryResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal MinScore { get; set; }
        public decimal MaxScore { get; set; }
        public string ColorHex { get; set; }
        public int DisplayOrder { get; set; }
    }
}