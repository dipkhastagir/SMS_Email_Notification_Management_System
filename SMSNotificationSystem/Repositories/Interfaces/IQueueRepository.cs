using SMSNotificationSystem.DTOs;
using SMSNotificationSystem.Models;

namespace SMSNotificationSystem.Repositories.Interfaces
{
    public interface IQueueRepository
    {
        Task<IEnumerable<QueueItemDto>> GetAllAsync(string? status = null, string? channel = null, int top = 300);
        Task<IDictionary<string, int>> GetStatusCountsAsync();

        /// <summary>Calls sp_EnqueueMessage. Returns the new id, or 0 when skipped as a duplicate.</summary>
        Task<int> EnqueueAsync(MessageQueue message, int cooldownHours = 24, bool oneTime = false);

        /// <summary>Calls sp_ClaimPendingMessages - marks a batch as Processing and returns it.</summary>
        Task<IEnumerable<MessageQueue>> ClaimPendingAsync(int batchSize);

        /// <summary>Calls sp_RecordDeliveryResult. Returns the message's new status.</summary>
        Task<string> RecordResultAsync(int messageId, GatewayResult result, int maxRetries);

        Task<bool> RequeueAsync(int messageId);
        Task<bool> CancelAsync(int messageId);
    }
}
