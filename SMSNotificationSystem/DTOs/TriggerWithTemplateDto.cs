namespace SMSNotificationSystem.DTOs
{
    /// <summary>One row from dbo.vw_TriggerDetails.</summary>
    public class TriggerWithTemplateDto
    {
        public int TriggerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public int TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string ConditionField { get; set; } = string.Empty;
        public string ConditionOperator { get; set; } = string.Empty;
        public string ConditionValue { get; set; } = string.Empty;
        public string? RecipientName { get; set; }
        public string? RecipientContact { get; set; }
        public int CooldownHours { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastRunAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public int MessagesQueued { get; set; }

        public string ConditionText => $"{ConditionField} {ConditionOperator} {ConditionValue}";
    }
}
