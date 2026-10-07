using SMSNotificationSystem.DTOs;

namespace SMSNotificationSystem.Repositories.Interfaces
{
    public interface IDeliveryLogRepository
    {
        Task<IEnumerable<DeliveryReportDto>> GetAllAsync(string? status = null, string? channel = null,
            string? search = null, DateTime? from = null, DateTime? to = null, int top = 500);
        Task<IEnumerable<DeliveryReportDto>> GetRecentAsync(int count);
        Task<IEnumerable<DeliveryReportDto>> GetByMessageIdAsync(int messageId);
    }
}
