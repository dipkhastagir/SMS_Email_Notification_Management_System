using Hangfire;
using SMSNotificationSystem.Services.Interfaces;

namespace SMSNotificationSystem.BackgroundJobs
{
    /// <summary>Hangfire job: sends queued messages through the configured gateways (every minute by default).</summary>
    public class DispatchJob
    {
        public const string JobId = "dispatch-queue";

        private readonly IDispatchService _dispatchService;
        private readonly ILogger<DispatchJob> _logger;

        public DispatchJob(IDispatchService dispatchService, ILogger<DispatchJob> logger)
        {
            _dispatchService = dispatchService;
            _logger = logger;
        }

        [DisableConcurrentExecution(timeoutInSeconds: 120)]
        [AutomaticRetry(Attempts = 0)]
        [JobDisplayName("Dispatch queued SMS and Email messages")]
        public async Task RunAsync()
        {
            try
            {
                var summary = await _dispatchService.ProcessQueueAsync();
                if (summary.Claimed > 0)
                    _logger.LogInformation("DispatchJob finished: {Summary}", summary);
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                // Database problems are logged instead of stopping the debugger every minute
                _logger.LogError(ex, "Background dispatch job could not reach the database: {Message}", ex.Message);
            }
        }
    }
}
