using SMSNotificationSystem.DTOs;

namespace SMSNotificationSystem.Services.Interfaces
{
    public interface IDispatchService
    {
        /// <summary>Claims a batch from the queue, sends each message and records the outcome.</summary>
        Task<DispatchSummary> ProcessQueueAsync(CancellationToken cancellationToken = default);
    }
}
