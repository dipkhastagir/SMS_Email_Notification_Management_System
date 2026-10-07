using SMSNotificationSystem.DTOs;

namespace SMSNotificationSystem.Services.Interfaces
{
    public interface ITriggerService
    {
        /// <summary>Evaluates every active trigger against business data and queues matching messages.</summary>
        Task<TriggerScanResult> EvaluateAllTriggersAsync();

        /// <summary>Builds sample placeholder values so users can preview a template.</summary>
        IReadOnlyDictionary<string, string> SampleValues(string? eventType);
    }
}
