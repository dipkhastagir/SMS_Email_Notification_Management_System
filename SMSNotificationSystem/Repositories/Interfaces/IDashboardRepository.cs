using SMSNotificationSystem.DTOs;

namespace SMSNotificationSystem.Repositories.Interfaces
{
    public interface IDashboardRepository
    {
        Task<DashboardStatsDto> GetStatsAsync();
        Task<IEnumerable<DailyDeliveryDto>> GetDailyStatsAsync(int days);
    }
}
