namespace Prestige.Api.ErrorResults
{
    public class NotFoundResult : BaseErrorResult
    {
        public NotFoundResult()
        {
            StatusCode = 404;
            Title = "Not Found";
            Message = "The resource does not exist.";
        }
    }
}
