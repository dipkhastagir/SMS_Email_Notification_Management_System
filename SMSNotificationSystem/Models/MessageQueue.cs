namespace SMSNotificationSystem.Models
{
    public class MessageQueue
    {
        public int MessageId { get; set; }
        public int? TriggerId { get; set; }
        public int? TemplateId { get; set; }
        public string? ReferenceKey { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientContact { get; set; } = string.Empty;
        public string Channel { get; set; } = "SMS";
        public string? Subject { get; set; }
        public string MessageBody { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";   // Pending | Processing | Sent | Retrying | Failed | Cancelled
        public int RetryCount { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastAttemptAt { get; set; }
    }
}
