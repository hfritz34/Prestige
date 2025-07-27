using System;

namespace Prestige.Api.Domain
{
    public class ImportHistory
    {
        public int Id { get; private set; }
        public string UserId { get; private set; }
        public string FileHash { get; private set; }
        public string FileName { get; private set; }
        public DateTime ImportDate { get; private set; }
        public int RecordCount { get; private set; }
        public DateTime? MinTimestamp { get; private set; }
        public DateTime? MaxTimestamp { get; private set; }
        public string Status { get; private set; }
        public string BatchId { get; private set; }

        private ImportHistory() { }

        public ImportHistory(string userId, string fileHash, string fileName, string batchId)
        {
            UserId = userId;
            FileHash = fileHash;
            FileName = fileName;
            BatchId = batchId;
            ImportDate = DateTime.UtcNow;
            Status = "Processing";
            RecordCount = 0;
        }

        public void SetCompleted(int recordCount, DateTime? minTimestamp, DateTime? maxTimestamp)
        {
            RecordCount = recordCount;
            MinTimestamp = minTimestamp;
            MaxTimestamp = maxTimestamp;
            Status = "Completed";
        }

        public void SetFailed(string reason)
        {
            Status = $"Failed: {reason}";
        }
    }
}