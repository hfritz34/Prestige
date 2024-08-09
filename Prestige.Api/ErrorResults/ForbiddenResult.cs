namespace Prestige.Api.ErrorResults
{
    public class ForbiddenResult : BaseErrorResult
    {
        public ForbiddenResult()
        {
            StatusCode = 403;
            Title = "Forbidden";
            Message = "The API key does not have permissions to perform the request.";
        }
    }
}
