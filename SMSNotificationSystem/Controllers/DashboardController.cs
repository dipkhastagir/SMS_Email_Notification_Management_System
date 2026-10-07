using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMSNotificationSystem.BackgroundJobs;
using SMSNotificationSystem.Helpers;
using SMSNotificationSystem.Repositories.Interfaces;
using SMSNotificationSystem.ViewModels;

namespace SMSNotificationSystem.Controllers
{
    [Authorize]
    public class DashboardController : AppController
    {
        private readonly IDashboardRepository _dashboardRepository;
        private readonly IDeliveryLogRepository _deliveryLogRepository;
        private readonly IQueueRepository _queueRepository;
        private readonly IBackgroundJobClient _jobs;

        public DashboardController(
            IDashboardRepository dashboardRepository,
            IDeliveryLogRepository deliveryLogRepository,
            IQueueRepository queueRepository,
            IBackgroundJobClient jobs)
        {
            _dashboardRepository = dashboardRepository;
            _deliveryLogRepository = deliveryLogRepository;
            _queueRepository = queueRepository;
            _jobs = jobs;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new DashboardViewModel
            {
                Stats = await _dashboardRepository.GetStatsAsync(),
                Daily = (await _dashboardRepository.GetDailyStatsAsync(7)).ToList(),
                RecentDeliveries = (await _deliveryLogRepository.GetRecentAsync(8)).ToList(),
                QueueCounts = await _queueRepository.GetStatusCountsAsync()
            };
            return View(vm);
        }

        /// <summary>Polled by the dashboard every 15 seconds to keep the numbers live.</summary>
        [HttpGet]
        public async Task<IActionResult> Stats()
        {
            var s = await _dashboardRepository.GetStatsAsync();
            return Json(new
            {
                s.ActiveTriggers,
                s.QueuedMessages,
                s.RetryingMessages,
                s.FailedMessages,
                s.SentToday,
                s.FailedAttemptsToday,
                s.SuccessRate
            });
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.AdminOrManager)]
        public IActionResult RunTriggerScan()
        {
            _jobs.Enqueue<TriggerScanJob>(job => job.RunAsync());
            Toast("Trigger scan started. New messages will appear in the queue within a few seconds.", "info");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.AdminOrManager)]
        public IActionResult DispatchNow()
        {
            _jobs.Enqueue<DispatchJob>(job => job.RunAsync());
            Toast("Dispatch started. Refresh in a few seconds to see delivery results.", "info");
            return RedirectToAction(nameof(Index));
        }
    }
}
