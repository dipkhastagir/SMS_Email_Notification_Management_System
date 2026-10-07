using Hangfire;
using SMSNotificationSystem.Services.Interfaces;

namespace SMSNotificationSystem.BackgroundJobs
{
    /// <summary>Hangfire job: scans business data against active triggers (every 5 minutes by default).</summary>
    public class TriggerScanJob
    {
        public const string JobId = "trigger-scan";

        private readonly ITriggerService _triggerService;
        private readonly ILogger<TriggerScanJob> _logger;

        public TriggerScanJob(ITriggerService triggerService, ILogger<TriggerScanJob> logger)
        {
            _triggerService = triggerService;
            _logger = logger;
        }

        [DisableConcurrentExecution(timeoutInSeconds: 300)]
        [AutomaticRetry(Attempts = 0)]
        [JobDisplayName("Scan business data for trigger matches")]
        public async Task RunAsync()
        {
            try
            {
                var result = await _triggerService.EvaluateAllTriggersAsync();
                _logger.LogInformation("TriggerScanJob finished: {Result}", result);
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                // Database problems are logged instead of stopping the debugger every minute
                _logger.LogError(ex, "Background scan job could not reach the database: {Message}", ex.Message);
            }
        }
    }
}
