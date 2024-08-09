namespace Prestige.Api.ErrorResults
{
    public abstract class BaseErrorResult
    {
        public int? StatusCode { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
        public string? TraceId { get; set; }
        public int? EventId { get; set; }
    }
}
