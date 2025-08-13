namespace Prestige.Api.Endpoints.Library.RequestResponse
{
    public class BatchItemResponse
    {
        public List<ItemDetailsResponse> Items { get; set; } = new List<ItemDetailsResponse>();
    }
}