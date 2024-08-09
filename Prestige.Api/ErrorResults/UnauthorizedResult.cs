namespace Prestige.Api.ErrorResults
{
    public class UnauthorizedResult : BaseErrorResult
    {
        public UnauthorizedResult()
        {
            StatusCode = 401;
            Title = "Unauthorized";
            Message = "Invalid API key or unauthorized access to resource.";
        }
    }
}
