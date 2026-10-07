using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMSNotificationSystem.Helpers;
using SMSNotificationSystem.Models;
using SMSNotificationSystem.Repositories.Interfaces;
using SMSNotificationSystem.ViewModels;

namespace SMSNotificationSystem.Controllers
{
    /// <summary>Gateway credentials are sensitive, so only Administrators can see or change them.</summary>
    [Authorize(Roles = AppRoles.Administrator)]
    public class GatewaySettingsController : AppController
    {
        private readonly IGatewaySettingRepository _gatewayRepository;

        public GatewaySettingsController(IGatewaySettingRepository gatewayRepository) => _gatewayRepository = gatewayRepository;

        public async Task<IActionResult> Index() => View(await _gatewayRepository.GetAllAsync());

        public IActionResult Create() => View("Form", new GatewayFormViewModel());

        [HttpPost]
        public async Task<IActionResult> Create(GatewayFormViewModel vm)
        {
            if (string.IsNullOrWhiteSpace(vm.ApiKey))
                ModelState.AddModelError(nameof(vm.ApiKey), "Enter the API key from your provider");
            ValidateChannel(vm);
            if (!ModelState.IsValid) return View("Form", vm);

            await _gatewayRepository.CreateAsync(new GatewaySetting
            {
                Provider = vm.Provider.Trim(),
                Channel = vm.Channel,
                ApiKey = vm.ApiKey!.Trim(),
                SenderId = vm.SenderId.Trim(),
                IsPrimary = vm.IsPrimary,
                IsActive = vm.IsActive
            });
            Toast("Gateway saved.");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var g = await _gatewayRepository.GetByIdAsync(id);
            if (g == null) return NotFound();

            return View("Form", new GatewayFormViewModel
            {
                GatewayId = g.GatewayId,
                Provider = g.Provider,
                Channel = g.Channel,
                SenderId = g.SenderId,
                IsPrimary = g.IsPrimary,
                IsActive = g.IsActive,
                MaskedApiKey = UiHelper.MaskSecret(g.ApiKey)
            });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(GatewayFormViewModel vm)
        {
            ValidateChannel(vm);
            var existing = await _gatewayRepository.GetByIdAsync(vm.GatewayId);
            if (existing == null) return NotFound();
            if (!ModelState.IsValid)
            {
                vm.MaskedApiKey = UiHelper.MaskSecret(existing.ApiKey);
                return View("Form", vm);
            }

            existing.Provider = vm.Provider.Trim();
            existing.Channel = vm.Channel;
            existing.SenderId = vm.SenderId.Trim();
            existing.IsPrimary = vm.IsPrimary;
            existing.IsActive = vm.IsActive;
            if (!string.IsNullOrWhiteSpace(vm.ApiKey)) existing.ApiKey = vm.ApiKey.Trim();

            await _gatewayRepository.UpdateAsync(existing);
            Toast("Gateway saved.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _gatewayRepository.DeleteAsync(id);
            Toast("Gateway removed.");
            return RedirectToAction(nameof(Index));
        }

        private void ValidateChannel(GatewayFormViewModel vm)
        {
            if (vm.Channel != "SMS" && vm.Channel != "Email")
                ModelState.AddModelError(nameof(vm.Channel), "Choose SMS or Email");
        }
    }
}
