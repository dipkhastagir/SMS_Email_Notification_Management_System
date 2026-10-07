namespace SMSNotificationSystem.DTOs
{
    /// <summary>One row from dbo.vw_DeliveryReport.</summary>
    public class DeliveryReportDto
    {
        public int LogId { get; set; }
        public int MessageId { get; set; }
        public int AttemptNo { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientContact { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string? EventType { get; set; }
        public string TriggerName { get; set; } = string.Empty;
        public string GatewayProvider { get; set; } = string.Empty;
        public string GatewayResponse { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? FailureReason { get; set; }
        public DateTime AttemptedAt { get; set; }
    }
}
