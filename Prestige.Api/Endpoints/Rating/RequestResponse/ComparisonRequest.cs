namespace Prestige.Api.Endpoints.Rating.RequestResponse
{
    public class ComparisonRequest
    {
        public string ItemId1 { get; set; }
        public string ItemId2 { get; set; }
        public string ItemType { get; set; }
        public string WinnerId { get; set; }
    }
}