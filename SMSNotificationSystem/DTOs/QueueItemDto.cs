namespace SMSNotificationSystem.DTOs
{
    /// <summary>One row from dbo.vw_QueueDetails.</summary>
    public class QueueItemDto
    {
        public int MessageId { get; set; }
        public int? TriggerId { get; set; }
        public string? TriggerName { get; set; }
        public string? EventType { get; set; }
        public string? ReferenceKey { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientContact { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string MessageBody { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int RetryCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastAttemptAt { get; set; }
        public string? CreatedByName { get; set; }

        public bool CanRetry => Status is "Failed" or "Cancelled";
        public bool CanCancel => Status is "Pending" or "Retrying";
    }
}
