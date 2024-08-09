namespace Prestige.Api.ErrorResults
{
    public class TooManyRequestsResult : BaseErrorResult
    {
        public TooManyRequestsResult()
        {
            StatusCode = 429;
            Title = "Too Many Requests";
            Message = "Rate limit error; too many requests hit the API too quickly. We recommend an exponential backoff of your requests.";
        }
    }
}
