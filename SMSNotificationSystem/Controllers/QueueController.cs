using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMSNotificationSystem.Helpers;
using SMSNotificationSystem.Models;
using SMSNotificationSystem.Repositories.Interfaces;
using SMSNotificationSystem.ViewModels;

namespace SMSNotificationSystem.Controllers
{
    [Authorize]
    public class QueueController : AppController
    {
        private readonly IQueueRepository _queueRepository;
        private readonly ITemplateRepository _templateRepository;
        private readonly IDeliveryLogRepository _deliveryLogRepository;

        public QueueController(IQueueRepository queueRepository, ITemplateRepository templateRepository,
            IDeliveryLogRepository deliveryLogRepository)
        {
            _queueRepository = queueRepository;
            _templateRepository = templateRepository;
            _deliveryLogRepository = deliveryLogRepository;
        }

        public async Task<IActionResult> Index(string? status = null, string? channel = null)
        {
            ViewBag.Status = status;
            ViewBag.Channel = channel;
            ViewBag.Counts = await _queueRepository.GetStatusCountsAsync();
            return View(await _queueRepository.GetAllAsync(status, channel));
        }

        /// <summary>Delivery attempts for one message, loaded into the details drawer.</summary>
        public async Task<IActionResult> Attempts(int id)
        {
            var attempts = await _deliveryLogRepository.GetByMessageIdAsync(id);
            return PartialView("_Attempts", attempts);
        }

        public async Task<IActionResult> ManualSend(int? templateId = null)
        {
            var vm = new ManualSendViewModel { Templates = await _templateRepository.GetActiveAsync() };
            if (templateId != null)
            {
                var t = vm.Templates.FirstOrDefault(x => x.TemplateId == templateId);
                if (t != null)
                {
                    vm.TemplateId = t.TemplateId;
                    vm.Channel = t.Channel;
                    vm.Subject = t.Subject;
                    vm.MessageBody = t.Body;
                }
            }
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> ManualSend(ManualSendViewModel vm)
        {
            if (vm.Channel != "SMS" && vm.Channel != "Email")
                ModelState.AddModelError(nameof(vm.Channel), "Choose SMS or Email");
            else if (!ContactValidator.IsValidFor(vm.Channel, vm.RecipientContact))
                ModelState.AddModelError(nameof(vm.RecipientContact),
                    vm.Channel == "Email" ? "Enter a valid email address" : "Enter a phone number like +8801711000101");
            if (vm.Channel == "Email" && string.IsNullOrWhiteSpace(vm.Subject))
                ModelState.AddModelError(nameof(vm.Subject), "Emails need a subject");

            if (!ModelState.IsValid)
            {
                vm.Templates = await _templateRepository.GetActiveAsync();
                return View(vm);
            }

            await _queueRepository.EnqueueAsync(new MessageQueue
            {
                TemplateId = vm.TemplateId,
                RecipientName = vm.RecipientName.Trim(),
                RecipientContact = ContactValidator.Normalize(vm.Channel, vm.RecipientContact),
                Channel = vm.Channel,
                Subject = vm.Channel == "Email" ? vm.Subject?.Trim() : null,
                MessageBody = vm.MessageBody.Trim().Replace("{RecipientName}", vm.RecipientName.Trim()),
                CreatedBy = User.GetUserId()
            });

            Toast($"Message to {vm.RecipientName} queued. It goes out on the next dispatch run.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Retry(int id)
        {
            var ok = await _queueRepository.RequeueAsync(id);
            Toast(ok ? $"Message #{id} is back in the queue." : "Only failed or cancelled messages can be retried.", ok ? "success" : "error");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var ok = await _queueRepository.CancelAsync(id);
            Toast(ok ? $"Message #{id} cancelled." : "This message is already being sent or finished.", ok ? "success" : "error");
            return RedirectToAction(nameof(Index));
        }
    }
}
