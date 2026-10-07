namespace SMSNotificationSystem.Models
{
    public class EventTrigger
    {
        public int TriggerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;       // InvoiceDue | StockLow | AttendanceAbsent | SalaryDisbursed
        public int TemplateId { get; set; }
        public string ConditionField { get; set; } = string.Empty;  // e.g. StockQty
        public string ConditionOperator { get; set; } = "<=";
        public string ConditionValue { get; set; } = string.Empty;  // literal or another field name
        public string? RecipientName { get; set; }                  // optional fixed recipient
        public string? RecipientContact { get; set; }
        public int CooldownHours { get; set; } = 24;
        public bool IsActive { get; set; } = true;
        public DateTime? LastRunAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
