namespace Prestige.Api.ErrorResults
{
    public class InternalServerErrorResult : BaseErrorResult
    {
        public InternalServerErrorResult()
        {
            StatusCode = 500;
            Title = "Internal Server Error";
            Message = "An error occurred while processing your request.";
        }
    }
}
