using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SMSNotificationSystem.Helpers;
using SMSNotificationSystem.Models;
using SMSNotificationSystem.Repositories.Interfaces;
using SMSNotificationSystem.ViewModels;

namespace SMSNotificationSystem.Controllers
{
    [Authorize]
    public class TriggerController : AppController
    {
        private readonly ITriggerRepository _triggerRepository;
        private readonly ITemplateRepository _templateRepository;

        public TriggerController(ITriggerRepository triggerRepository, ITemplateRepository templateRepository)
        {
            _triggerRepository = triggerRepository;
            _templateRepository = templateRepository;
        }

        public async Task<IActionResult> Index() => View(await _triggerRepository.GetAllAsync());

        [Authorize(Roles = AppRoles.AdminOrManager)]
        public async Task<IActionResult> Create(string? eventType = null)
        {
            var vm = new TriggerFormViewModel();
            if (eventType != null && EventTypes.All.TryGetValue(eventType, out var info))
            {
                vm.EventType = info.Key;
                vm.ConditionField = info.DefaultField;
                vm.ConditionOperator = info.DefaultOperator;
                vm.ConditionValue = info.DefaultValue;
            }
            await FillTemplatesAsync(vm);
            return View("Form", vm);
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.AdminOrManager)]
        public async Task<IActionResult> Create(TriggerFormViewModel vm)
        {
            await ValidateAsync(vm);
            if (!ModelState.IsValid)
            {
                await FillTemplatesAsync(vm);
                return View("Form", vm);
            }

            await _triggerRepository.CreateAsync(ToEntity(vm));
            Toast("Trigger saved. It will be checked on the next scan.");
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = AppRoles.AdminOrManager)]
        public async Task<IActionResult> Edit(int id)
        {
            var t = await _triggerRepository.GetByIdAsync(id);
            if (t == null) return NotFound();

            var vm = new TriggerFormViewModel
            {
                TriggerId = t.TriggerId,
                Name = t.Name,
                EventType = t.EventType,
                TemplateId = t.TemplateId,
                ConditionField = t.ConditionField,
                ConditionOperator = t.ConditionOperator,
                ConditionValue = t.ConditionValue,
                RecipientName = t.RecipientName,
                RecipientContact = t.RecipientContact,
                CooldownHours = t.CooldownHours,
                IsActive = t.IsActive
            };
            await FillTemplatesAsync(vm);
            return View("Form", vm);
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.AdminOrManager)]
        public async Task<IActionResult> Edit(TriggerFormViewModel vm)
        {
            await ValidateAsync(vm);
            if (!ModelState.IsValid)
            {
                await FillTemplatesAsync(vm);
                return View("Form", vm);
            }

            var updated = await _triggerRepository.UpdateAsync(ToEntity(vm));
            if (!updated) return NotFound();

            Toast("Trigger saved.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.AdminOrManager)]
        public async Task<IActionResult> Toggle(int id)
        {
            var t = await _triggerRepository.GetByIdAsync(id);
            if (t == null) return NotFound();

            await _triggerRepository.SetActiveAsync(id, !t.IsActive);
            Toast(t.IsActive ? $"\"{t.Name}\" paused." : $"\"{t.Name}\" resumed.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.AdminOrManager)]
        public async Task<IActionResult> Delete(int id)
        {
            await _triggerRepository.DeleteAsync(id);
            Toast("Trigger deleted. Messages it already queued are kept.");
            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateAsync(TriggerFormViewModel vm)
        {
            if (!EventTypes.All.TryGetValue(vm.EventType ?? string.Empty, out var info))
            {
                ModelState.AddModelError(nameof(vm.EventType), "Choose a supported event");
                return;
            }

            if (!info.Fields.Contains(vm.ConditionField))
                ModelState.AddModelError(nameof(vm.ConditionField), $"For {info.DisplayName}, use one of: {string.Join(", ", info.Fields)}");

            if (!EventTypes.Operators.Contains(vm.ConditionOperator))
                ModelState.AddModelError(nameof(vm.ConditionOperator), "Choose a valid operator");

            var template = await _templateRepository.GetByIdAsync(vm.TemplateId);
            if (template == null)
            {
                ModelState.AddModelError(nameof(vm.TemplateId), "Choose a template");
                return;
            }

            if (vm.EventType == EventTypes.StockLow && string.IsNullOrWhiteSpace(vm.RecipientContact))
                ModelState.AddModelError(nameof(vm.RecipientContact), "Low stock alerts need someone to send them to");

            if (!string.IsNullOrWhiteSpace(vm.RecipientContact) && !ContactValidator.IsValidFor(template.Channel, vm.RecipientContact))
                ModelState.AddModelError(nameof(vm.RecipientContact),
                    template.Channel == "Email"
                        ? "This template sends Email, so enter an email address"
                        : "This template sends SMS, so enter a phone number like +8801711000101");
        }

        private static EventTrigger ToEntity(TriggerFormViewModel vm) => new()
        {
            TriggerId = vm.TriggerId,
            Name = vm.Name.Trim(),
            EventType = vm.EventType,
            TemplateId = vm.TemplateId,
            ConditionField = vm.ConditionField,
            ConditionOperator = vm.ConditionOperator,
            ConditionValue = vm.ConditionValue.Trim(),
            RecipientName = string.IsNullOrWhiteSpace(vm.RecipientName) ? null : vm.RecipientName.Trim(),
            RecipientContact = string.IsNullOrWhiteSpace(vm.RecipientContact) ? null : vm.RecipientContact.Trim(),
            CooldownHours = vm.CooldownHours,
            IsActive = vm.IsActive
        };

        private async Task FillTemplatesAsync(TriggerFormViewModel vm)
        {
            var all = await _templateRepository.GetAllAsync();
            vm.Templates = all.Select(t => new SelectListItem(
                $"{t.Name} ({t.Channel}{(t.Status == "Active" ? "" : ", inactive")})",
                t.TemplateId.ToString(),
                t.TemplateId == vm.TemplateId));
            vm.TemplateChannels = all.ToDictionary(t => t.TemplateId, t => t.Channel);
        }
    }
}
