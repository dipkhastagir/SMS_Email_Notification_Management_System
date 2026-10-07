namespace SMSNotificationSystem.Models
{
    public class DeliveryLog
    {
        public int LogId { get; set; }
        public int MessageId { get; set; }
        public int AttemptNo { get; set; }
        public string GatewayProvider { get; set; } = string.Empty;
        public string GatewayResponse { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;   // Sent | Failed
        public string? FailureReason { get; set; }
        public DateTime AttemptedAt { get; set; } = DateTime.Now;
    }
}
