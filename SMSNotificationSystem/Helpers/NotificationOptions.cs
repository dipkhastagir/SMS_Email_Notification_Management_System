namespace SMSNotificationSystem.Helpers
{
    /// <summary>Bound from the "Notification" section of appsettings.json.</summary>
    public class NotificationOptions
    {
        public const string SectionName = "Notification";

        public string CompanyName { get; set; } = "Best Business Bond Ltd.";
        public int MaxRetries { get; set; } = 3;
        public int DispatchBatchSize { get; set; } = 25;
        public int SimulatedSuccessRate { get; set; } = 90;
        public string TriggerScanCron { get; set; } = "*/5 * * * *";
        public string DispatchCron { get; set; } = "* * * * *";
    }
}
