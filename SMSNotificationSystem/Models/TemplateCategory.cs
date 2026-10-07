namespace SMSNotificationSystem.Models
{
    public class TemplateCategory
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>Not a table column - filled by the listing query.</summary>
        public int TemplateCount { get; set; }
    }
}
