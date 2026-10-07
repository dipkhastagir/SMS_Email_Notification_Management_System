using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMSNotificationSystem.Repositories.Interfaces;

namespace SMSNotificationSystem.Controllers
{
    [Authorize]
    public class DeliveryLogController : AppController
    {
        private readonly IDeliveryLogRepository _deliveryLogRepository;

        public DeliveryLogController(IDeliveryLogRepository deliveryLogRepository) => _deliveryLogRepository = deliveryLogRepository;

        public async Task<IActionResult> Index(string? status, string? channel, string? search, DateTime? from, DateTime? to)
        {
            ViewBag.Status = status;
            ViewBag.Channel = channel;
            ViewBag.Search = search;
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");
            return View(await _deliveryLogRepository.GetAllAsync(status, channel, search, from, to));
        }

        /// <summary>Downloads the filtered delivery log as a CSV file (opens in Excel).</summary>
        public async Task<IActionResult> ExportCsv(string? status, string? channel, string? search, DateTime? from, DateTime? to)
        {
            var rows = await _deliveryLogRepository.GetAllAsync(status, channel, search, from, to, top: 10000);

            var sb = new StringBuilder();
            sb.AppendLine("LogId,MessageId,Attempt,AttemptedAt,Recipient,Contact,Channel,Trigger,Gateway,Status,FailureReason,GatewayResponse");
            foreach (var r in rows)
            {
                sb.AppendLine(string.Join(",",
                    r.LogId, r.MessageId, r.AttemptNo, r.AttemptedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    Csv(r.RecipientName), Csv(r.RecipientContact), r.Channel, Csv(r.TriggerName),
                    Csv(r.GatewayProvider), r.Status, Csv(r.FailureReason), Csv(r.GatewayResponse)));
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"delivery-log-{DateTime.Now:yyyyMMdd-HHmm}.csv");
        }

        private static string Csv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var needsQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\n');
            var escaped = value.Replace("\"", "\"\"");
            return needsQuotes ? $"\"{escaped}\"" : escaped;
        }
    }
}
