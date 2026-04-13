using System;
using System.Collections.Generic;

namespace Prestige.Api.Domain
{
    public class UserConsent
    {
        public string UserId { get; private set; }
        public bool AllowsPersonalInsights { get; private set; }
        public bool AllowsDerivedFeatureStorage { get; private set; }
        public bool AllowsAnonymizedAggregation { get; private set; }
        public bool AllowsModelTraining { get; private set; }
        public DateTime LastUpdatedAt { get; private set; }
        public string ConsentVersion { get; private set; }
        public List<ConsentEvent> History { get; private set; } = new();

        private UserConsent() { }

        public UserConsent(string userId, string consentVersion)
        {
            UserId = userId;
            ConsentVersion = consentVersion;
            AllowsPersonalInsights = true;
            AllowsDerivedFeatureStorage = true;
            AllowsAnonymizedAggregation = false;
            AllowsModelTraining = false;
            LastUpdatedAt = DateTime.UtcNow;

            RecordInitialDefaults();
        }

        public void UpdateResearchPreferences(
            bool? allowsAnonymizedAggregation,
            bool? allowsModelTraining,
            string triggeredBy,
            string consentVersion)
        {
            var normalizedTrigger = string.IsNullOrWhiteSpace(triggeredBy) ? "settings_toggle" : triggeredBy;

            UpdateAllowsAnonymizedAggregation(allowsAnonymizedAggregation, normalizedTrigger);
            UpdateAllowsModelTraining(allowsModelTraining, normalizedTrigger);

            ConsentVersion = string.IsNullOrWhiteSpace(consentVersion) ? ConsentVersion : consentVersion;
            LastUpdatedAt = DateTime.UtcNow;
        }

        private void RecordInitialDefaults()
        {
            History.Add(new ConsentEvent(UserId, nameof(AllowsPersonalInsights), false, AllowsPersonalInsights, "signup"));
            History.Add(new ConsentEvent(UserId, nameof(AllowsDerivedFeatureStorage), false, AllowsDerivedFeatureStorage, "signup"));
            History.Add(new ConsentEvent(UserId, nameof(AllowsAnonymizedAggregation), false, AllowsAnonymizedAggregation, "signup"));
            History.Add(new ConsentEvent(UserId, nameof(AllowsModelTraining), false, AllowsModelTraining, "signup"));
        }

        private void UpdateAllowsAnonymizedAggregation(bool? newValue, string triggeredBy)
        {
            if (!newValue.HasValue || AllowsAnonymizedAggregation == newValue.Value)
            {
                return;
            }

            History.Add(new ConsentEvent(UserId, nameof(AllowsAnonymizedAggregation), AllowsAnonymizedAggregation, newValue.Value, triggeredBy));
            AllowsAnonymizedAggregation = newValue.Value;
        }

        private void UpdateAllowsModelTraining(bool? newValue, string triggeredBy)
        {
            if (!newValue.HasValue || AllowsModelTraining == newValue.Value)
            {
                return;
            }

            History.Add(new ConsentEvent(UserId, nameof(AllowsModelTraining), AllowsModelTraining, newValue.Value, triggeredBy));
            AllowsModelTraining = newValue.Value;
        }
    }

    public class ConsentEvent
    {
        public int Id { get; private set; }
        public string UserId { get; private set; }
        public DateTime Timestamp { get; private set; }
        public string Field { get; private set; }
        public bool OldValue { get; private set; }
        public bool NewValue { get; private set; }
        public string TriggeredBy { get; private set; }

        private ConsentEvent() { }

        public ConsentEvent(string userId, string field, bool oldValue, bool newValue, string triggeredBy)
        {
            UserId = userId;
            Field = field;
            OldValue = oldValue;
            NewValue = newValue;
            TriggeredBy = triggeredBy;
            Timestamp = DateTime.UtcNow;
        }
    }
}
