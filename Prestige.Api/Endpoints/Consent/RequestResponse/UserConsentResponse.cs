using Prestige.Api.Domain;

namespace Prestige.Api.Endpoints.Consent.RequestResponse
{
    public class UserConsentResponse
    {
        public string UserId { get; set; }
        public bool AllowsPersonalInsights { get; set; }
        public bool AllowsDerivedFeatureStorage { get; set; }
        public bool AllowsAnonymizedAggregation { get; set; }
        public bool AllowsModelTraining { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public string ConsentVersion { get; set; }
        public List<ConsentEventResponse> History { get; set; }

        public UserConsentResponse(UserConsent consent)
        {
            UserId = consent.UserId;
            AllowsPersonalInsights = consent.AllowsPersonalInsights;
            AllowsDerivedFeatureStorage = consent.AllowsDerivedFeatureStorage;
            AllowsAnonymizedAggregation = consent.AllowsAnonymizedAggregation;
            AllowsModelTraining = consent.AllowsModelTraining;
            LastUpdatedAt = consent.LastUpdatedAt;
            ConsentVersion = consent.ConsentVersion;
            History = consent.History
                .OrderByDescending(item => item.Timestamp)
                .Select(item => new ConsentEventResponse(item))
                .ToList();
        }
    }

    public class ConsentEventResponse
    {
        public DateTime Timestamp { get; set; }
        public string Field { get; set; }
        public bool OldValue { get; set; }
        public bool NewValue { get; set; }
        public string TriggeredBy { get; set; }

        public ConsentEventResponse(ConsentEvent consentEvent)
        {
            Timestamp = consentEvent.Timestamp;
            Field = consentEvent.Field;
            OldValue = consentEvent.OldValue;
            NewValue = consentEvent.NewValue;
            TriggeredBy = consentEvent.TriggeredBy;
        }
    }
}
