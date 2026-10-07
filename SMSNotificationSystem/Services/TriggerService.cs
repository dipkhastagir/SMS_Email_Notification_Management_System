using System.Globalization;
using Microsoft.Extensions.Options;
using SMSNotificationSystem.DTOs;
using SMSNotificationSystem.Helpers;
using SMSNotificationSystem.Models;
using SMSNotificationSystem.Repositories.Interfaces;
using SMSNotificationSystem.Services.Interfaces;

namespace SMSNotificationSystem.Services
{
    /// <summary>
    /// Business data -> trigger evaluation -> template placeholder fill -> message queue.
    /// Runs on a Hangfire schedule (see BackgroundJobs/TriggerScanJob.cs).
    /// </summary>
    public class TriggerService : ITriggerService
    {
        /// <summary>One business record that a trigger can be evaluated against.</summary>
        private sealed record Candidate(
            string ReferenceKey,
            string RecipientName,
            string? Phone,
            string? Email,
            Dictionary<string, object?> Fields,
            Dictionary<string, string> Values);

        private readonly ITriggerRepository _triggerRepository;
        private readonly ITemplateRepository _templateRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IProductRepository _productRepository;
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IPayrollRepository _payrollRepository;
        private readonly IQueueRepository _queueRepository;
        private readonly NotificationOptions _options;
        private readonly ILogger<TriggerService> _logger;

        public TriggerService(
            ITriggerRepository triggerRepository,
            ITemplateRepository templateRepository,
            IInvoiceRepository invoiceRepository,
            IProductRepository productRepository,
            IAttendanceRepository attendanceRepository,
            IPayrollRepository payrollRepository,
            IQueueRepository queueRepository,
            IOptions<NotificationOptions> options,
            ILogger<TriggerService> logger)
        {
            _triggerRepository = triggerRepository;
            _templateRepository = templateRepository;
            _invoiceRepository = invoiceRepository;
            _productRepository = productRepository;
            _attendanceRepository = attendanceRepository;
            _payrollRepository = payrollRepository;
            _queueRepository = queueRepository;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<TriggerScanResult> EvaluateAllTriggersAsync()
        {
            var result = new TriggerScanResult();
            var triggers = (await _triggerRepository.GetActiveAsync()).ToList();
            if (triggers.Count == 0) return result;

            // Load each kind of business data at most once per scan
            var cache = new Dictionary<string, List<Candidate>>();

            foreach (var trigger in triggers)
            {
                var template = await _templateRepository.GetByIdAsync(trigger.TemplateId);
                if (template == null || template.Status != "Active")
                {
                    _logger.LogInformation("Trigger {TriggerId} skipped: template inactive or missing", trigger.TriggerId);
                    continue;
                }

                if (!cache.TryGetValue(trigger.EventType, out var candidates))
                {
                    candidates = await LoadCandidatesAsync(trigger.EventType);
                    cache[trigger.EventType] = candidates;
                }

                result.TriggersEvaluated++;
                var oneTime = EventTypes.All.TryGetValue(trigger.EventType, out var info) && info.OneTimePerRecord;

                foreach (var candidate in candidates)
                {
                    if (!ConditionEvaluator.Evaluate(candidate.Fields, trigger.ConditionField,
                            trigger.ConditionOperator, trigger.ConditionValue))
                        continue;

                    result.RecordsMatched++;

                    // A fixed recipient on the trigger (e.g. store manager) wins over the record's own contact
                    var hasFixedRecipient = !string.IsNullOrWhiteSpace(trigger.RecipientContact);
                    var recipientName = hasFixedRecipient
                        ? (string.IsNullOrWhiteSpace(trigger.RecipientName) ? "Alert recipient" : trigger.RecipientName!)
                        : candidate.RecipientName;
                    var contact = hasFixedRecipient
                        ? trigger.RecipientContact
                        : template.Channel == "Email" ? candidate.Email : candidate.Phone;

                    if (string.IsNullOrWhiteSpace(contact))
                    {
                        result.MissingContact++;
                        continue;
                    }

                    var values = TemplateRenderer.GlobalValues(_options.CompanyName, recipientName);
                    foreach (var pair in candidate.Values) values[pair.Key] = pair.Value;

                    var message = new MessageQueue
                    {
                        TriggerId = trigger.TriggerId,
                        TemplateId = template.TemplateId,
                        ReferenceKey = candidate.ReferenceKey,
                        RecipientName = recipientName,
                        RecipientContact = ContactValidator.Normalize(template.Channel, contact),
                        Channel = template.Channel,
                        Subject = template.Channel == "Email" ? TemplateRenderer.Render(template.Subject, values) : null,
                        MessageBody = TemplateRenderer.Render(template.Body, values)
                    };

                    var id = await _queueRepository.EnqueueAsync(message, trigger.CooldownHours, oneTime);
                    if (id > 0) result.MessagesQueued++;
                    else result.DuplicatesSkipped++;
                }

                await _triggerRepository.UpdateLastRunAsync(trigger.TriggerId);
            }

            _logger.LogInformation("Trigger scan: {Result}", result);
            return result;
        }

        private async Task<List<Candidate>> LoadCandidatesAsync(string eventType)
        {
            var list = new List<Candidate>();
            var today = DateTime.Today;

            switch (eventType)
            {
                case EventTypes.InvoiceDue:
                    foreach (var inv in await _invoiceRepository.GetUnpaidAsync())
                    {
                        var daysLeft = (inv.DueDate.Date - today).Days;
                        list.Add(new Candidate($"INV-{inv.InvoiceId}", inv.CustomerName, inv.CustomerPhone, inv.CustomerEmail,
                            new Dictionary<string, object?>
                            {
                                ["DaysUntilDue"] = daysLeft,
                                ["Amount"] = inv.Amount
                            },
                            new Dictionary<string, string>
                            {
                                ["CustomerName"] = inv.CustomerName,
                                ["InvoiceNo"] = inv.InvoiceNo,
                                ["DueAmount"] = inv.Amount.ToString("N2", CultureInfo.InvariantCulture),
                                ["DueDate"] = inv.DueDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture),
                                ["DaysLeft"] = Math.Max(0, daysLeft).ToString(CultureInfo.InvariantCulture)
                            }));
                    }
                    break;

                case EventTypes.StockLow:
                    foreach (var p in await _productRepository.GetAllAsync())
                    {
                        list.Add(new Candidate($"PRD-{p.ProductId}", p.ProductName, null, null,
                            new Dictionary<string, object?>
                            {
                                ["StockQty"] = p.StockQty,
                                ["ReorderLevel"] = p.ReorderLevel
                            },
                            new Dictionary<string, string>
                            {
                                ["ProductName"] = p.ProductName,
                                ["Sku"] = p.Sku ?? "-",
                                ["StockQty"] = p.StockQty.ToString(CultureInfo.InvariantCulture),
                                ["ReorderLevel"] = p.ReorderLevel.ToString(CultureInfo.InvariantCulture)
                            }));
                    }
                    break;

                case EventTypes.AttendanceAbsent:
                    foreach (var a in await _attendanceRepository.GetByDateAsync(today))
                    {
                        list.Add(new Candidate($"ATT-{a.AttendanceId}-{a.Status}", a.EmployeeName, a.Phone, a.Email,
                            new Dictionary<string, object?>
                            {
                                ["Status"] = a.Status
                            },
                            new Dictionary<string, string>
                            {
                                ["EmployeeName"] = a.EmployeeName,
                                ["Department"] = a.Department,
                                ["Date"] = a.AttendanceDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture),
                                ["Status"] = a.Status.ToLowerInvariant()
                            }));
                    }
                    break;

                case EventTypes.SalaryDisbursed:
                    foreach (var s in await _payrollRepository.GetRecentAsync(3))
                    {
                        list.Add(new Candidate($"SAL-{s.PaymentId}", s.EmployeeName, s.Phone, s.Email,
                            new Dictionary<string, object?>
                            {
                                ["Amount"] = s.Amount
                            },
                            new Dictionary<string, string>
                            {
                                ["EmployeeName"] = s.EmployeeName,
                                ["Amount"] = s.Amount.ToString("N2", CultureInfo.InvariantCulture),
                                ["Month"] = MonthLabel(s.SalaryMonth)
                            }));
                    }
                    break;
            }

            return list;
        }

        public IReadOnlyDictionary<string, string> SampleValues(string? eventType)
        {
            var values = TemplateRenderer.GlobalValues(_options.CompanyName, "Rahim Traders");
            var sample = eventType switch
            {
                EventTypes.InvoiceDue => new Dictionary<string, string>
                {
                    ["CustomerName"] = "Rahim Traders", ["InvoiceNo"] = "INV-2026-0001", ["DueAmount"] = "45,000.00",
                    ["DueDate"] = DateTime.Today.AddDays(2).ToString("dd MMM yyyy"), ["DaysLeft"] = "2"
                },
                EventTypes.StockLow => new Dictionary<string, string>
                {
                    ["ProductName"] = "Toner Cartridge 85A", ["Sku"] = "PRN-85A", ["StockQty"] = "3", ["ReorderLevel"] = "5"
                },
                EventTypes.AttendanceAbsent => new Dictionary<string, string>
                {
                    ["EmployeeName"] = "Mehedi Rahman", ["Department"] = "Support",
                    ["Date"] = DateTime.Today.ToString("dd MMM yyyy"), ["Status"] = "absent"
                },
                EventTypes.SalaryDisbursed => new Dictionary<string, string>
                {
                    ["EmployeeName"] = "Arif Hossain", ["Amount"] = "65,000.00", ["Month"] = DateTime.Today.ToString("MMMM yyyy")
                },
                _ => new Dictionary<string, string>()
            };
            foreach (var pair in sample) values[pair.Key] = pair.Value;
            return values;
        }

        private static string MonthLabel(string salaryMonth) =>
            DateTime.TryParseExact(salaryMonth + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                ? d.ToString("MMMM yyyy", CultureInfo.InvariantCulture)
                : salaryMonth;
    }
}
