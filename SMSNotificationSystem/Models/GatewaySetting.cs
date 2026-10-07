namespace SMSNotificationSystem.Models
{
    public class GatewaySetting
    {
        public int GatewayId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string Channel { get; set; } = "SMS";
        public string ApiKey { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
