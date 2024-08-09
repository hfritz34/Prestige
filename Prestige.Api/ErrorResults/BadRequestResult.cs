namespace Prestige.Api.ErrorResults
{
    public class BadRequestResult : BaseErrorResult
    {
        public BadRequestResult()
        {
            StatusCode = 400;
            Title = "Bad Request";
            Message = "The request could not be understood due to malformed input.";
        }
    }
}
