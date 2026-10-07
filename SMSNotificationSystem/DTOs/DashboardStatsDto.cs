namespace SMSNotificationSystem.DTOs
{
    /// <summary>One row from dbo.vw_DashboardStats.</summary>
    public class DashboardStatsDto
    {
        public int ActiveTriggers { get; set; }
        public int ActiveTemplates { get; set; }
        public int QueuedMessages { get; set; }
        public int RetryingMessages { get; set; }
        public int FailedMessages { get; set; }
        public int SentToday { get; set; }
        public int FailedAttemptsToday { get; set; }
        public int TotalSent { get; set; }
        public int TotalAttempts { get; set; }
        public int LowStockItems { get; set; }
        public int UnpaidInvoices { get; set; }

        public double SuccessRate => TotalAttempts == 0 ? 0 : Math.Round(100.0 * TotalSent / TotalAttempts, 1);
    }
}
