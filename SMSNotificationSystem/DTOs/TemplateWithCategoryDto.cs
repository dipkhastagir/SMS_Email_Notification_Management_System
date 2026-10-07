namespace SMSNotificationSystem.DTOs
{
    /// <summary>One row from dbo.vw_TemplateDetails.</summary>
    public class TemplateWithCategoryDto
    {
        public int TemplateId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string Body { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedByName { get; set; }
        public int TriggerCount { get; set; }
    }
}
