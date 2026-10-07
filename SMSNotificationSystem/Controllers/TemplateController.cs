using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using SMSNotificationSystem.Helpers;
using SMSNotificationSystem.Models;
using SMSNotificationSystem.Repositories.Interfaces;
using SMSNotificationSystem.ViewModels;

namespace SMSNotificationSystem.Controllers
{
    [Authorize]
    public class TemplateController : AppController
    {
        private readonly ITemplateRepository _templateRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IQueueRepository _queueRepository;
        private readonly NotificationOptions _options;

        public TemplateController(
            ITemplateRepository templateRepository,
            ICategoryRepository categoryRepository,
            IQueueRepository queueRepository,
            IOptions<NotificationOptions> options)
        {
            _templateRepository = templateRepository;
            _categoryRepository = categoryRepository;
            _queueRepository = queueRepository;
            _options = options.Value;
        }

        public async Task<IActionResult> Index(string? channel = null)
        {
            var templates = await _templateRepository.GetAllAsync();
            if (!string.IsNullOrEmpty(channel))
                templates = templates.Where(t => t.Channel == channel);
            ViewBag.Channel = channel;
            return View(templates);
        }

        [Authorize(Roles = AppRoles.AdminOrManager)]
        public async Task<IActionResult> Create()
        {
            var vm = new TemplateFormViewModel();
            await FillCategoriesAsync(vm);
            return View("Form", vm);
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.AdminOrManager)]
        public async Task<IActionResult> Create(TemplateFormViewModel vm)
        {
            Validate(vm);
            if (!ModelState.IsValid)
            {
                await FillCategoriesAsync(vm);
                return View("Form", vm);
            }

            await _templateRepository.CreateAsync(new MessageTemplate
            {
                Name = vm.Name.Trim(),
                CategoryId = vm.CategoryId,
                Channel = vm.Channel,
                Subject = vm.Channel == "Email" ? vm.Subject?.Trim() : null,
                Body = vm.Body.Trim(),
                Status = vm.Status,
                CreatedBy = User.GetUserId()
            });

            Toast("Template saved.");
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = AppRoles.AdminOrManager)]
        public async Task<IActionResult> Edit(int id)
        {
            var t = await _templateRepository.GetByIdAsync(id);
            if (t == null) return NotFound();

            var vm = new TemplateFormViewModel
            {
                TemplateId = t.TemplateId,
                Name = t.Name,
                CategoryId = t.CategoryId,
                Channel = t.Channel,
                Subject = t.Subject,
                Body = t.Body,
                Status = t.Status
            };
            await FillCategoriesAsync(vm);
            return View("Form", vm);
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.AdminOrManager)]
        public async Task<IActionResult> Edit(TemplateFormViewModel vm)
        {
            Validate(vm);
            if (!ModelState.IsValid)
            {
                await FillCategoriesAsync(vm);
                return View("Form", vm);
            }

            var updated = await _templateRepository.UpdateAsync(new MessageTemplate
            {
                TemplateId = vm.TemplateId,
                Name = vm.Name.Trim(),
                CategoryId = vm.CategoryId,
                Channel = vm.Channel,
                Subject = vm.Channel == "Email" ? vm.Subject?.Trim() : null,
                Body = vm.Body.Trim(),
                Status = vm.Status
            });
            if (!updated) return NotFound();

            Toast("Template saved.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.AdminOrManager)]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var t = await _templateRepository.GetByIdAsync(id);
            if (t == null) return NotFound();

            var next = t.Status == "Active" ? "Inactive" : "Active";
            await _templateRepository.SetStatusAsync(id, next);
            Toast(next == "Active" ? $"\"{t.Name}\" is active again." : $"\"{t.Name}\" paused. Triggers using it will skip until it's active.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.AdminOrManager)]
        public async Task<IActionResult> Delete(int id)
        {
            if (await _templateRepository.IsUsedByTriggerAsync(id))
            {
                Toast("A trigger still uses this template. Change or delete that trigger first.", "error");
                return RedirectToAction(nameof(Index));
            }

            await _templateRepository.DeleteAsync(id);
            Toast("Template deleted.");
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Queues one message rendered from this template with sample values.</summary>
        [HttpPost]
        public async Task<IActionResult> SendTest(int id, string contact)
        {
            var t = await _templateRepository.GetByIdAsync(id);
            if (t == null) return NotFound();

            if (!ContactValidator.IsValidFor(t.Channel, contact))
            {
                Toast(t.Channel == "Email" ? "Enter a valid email address for the test." : "Enter a valid phone number for the test.", "error");
                return RedirectToAction(nameof(Index));
            }

            var values = TemplateRenderer.GlobalValues(_options.CompanyName, User.Identity?.Name ?? "Test recipient");
            await _queueRepository.EnqueueAsync(new MessageQueue
            {
                TemplateId = t.TemplateId,
                RecipientName = User.Identity?.Name ?? "Test recipient",
                RecipientContact = ContactValidator.Normalize(t.Channel, contact),
                Channel = t.Channel,
                Subject = t.Channel == "Email" ? "[TEST] " + TemplateRenderer.Render(t.Subject, values) : null,
                MessageBody = TemplateRenderer.Render(t.Body, values),
                CreatedBy = User.GetUserId()
            });

            Toast($"Test message queued to {contact}. It will be sent on the next dispatch run.", "info");
            return RedirectToAction("Index", "Queue");
        }

        private void Validate(TemplateFormViewModel vm)
        {
            if (vm.Channel != "SMS" && vm.Channel != "Email")
                ModelState.AddModelError(nameof(vm.Channel), "Choose SMS or Email");
            if (vm.Status != "Active" && vm.Status != "Inactive")
                ModelState.AddModelError(nameof(vm.Status), "Choose Active or Inactive");
            if (vm.Channel == "Email" && string.IsNullOrWhiteSpace(vm.Subject))
                ModelState.AddModelError(nameof(vm.Subject), "Email templates need a subject");
        }

        private async Task FillCategoriesAsync(TemplateFormViewModel vm)
        {
            var categories = await _categoryRepository.GetAllAsync();
            vm.Categories = categories.Select(c => new SelectListItem(c.Name, c.CategoryId.ToString(), c.CategoryId == vm.CategoryId));
        }
    }
}
