using SMSNotificationSystem.DTOs;

namespace SMSNotificationSystem.ViewModels
{
    public class DashboardViewModel
    {
        public DashboardStatsDto Stats { get; set; } = new();
        public List<DailyDeliveryDto> Daily { get; set; } = new();
        public List<DeliveryReportDto> RecentDeliveries { get; set; } = new();
        public IDictionary<string, int> QueueCounts { get; set; } = new Dictionary<string, int>();

        public int ChartMax => Math.Max(1, Daily.Count == 0 ? 1 : Daily.Max(d => d.SentCount + d.FailedCount));
    }
}
