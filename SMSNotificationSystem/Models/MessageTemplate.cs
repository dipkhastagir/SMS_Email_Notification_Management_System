namespace SMSNotificationSystem.Models
{
    public class MessageTemplate
    {
        public int TemplateId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string Channel { get; set; } = "SMS";          // SMS | Email
        public string? Subject { get; set; }                  // Email only
        public string Body { get; set; } = string.Empty;      // contains {Placeholders}
        public string Status { get; set; } = "Active";        // Active | Inactive
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
