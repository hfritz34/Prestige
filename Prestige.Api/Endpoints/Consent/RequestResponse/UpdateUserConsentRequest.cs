namespace Prestige.Api.Endpoints.Consent.RequestResponse
{
    public class UpdateUserConsentRequest
    {
        public bool? AllowsAnonymizedAggregation { get; set; }
        public bool? AllowsModelTraining { get; set; }
        public string? ConsentVersion { get; set; }
        public string? TriggeredBy { get; set; }
    }
}
