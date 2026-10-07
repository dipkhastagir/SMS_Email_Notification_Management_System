using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMSNotificationSystem.Helpers;
using SMSNotificationSystem.Models;
using SMSNotificationSystem.Repositories.Interfaces;
using SMSNotificationSystem.ViewModels;

namespace SMSNotificationSystem.Controllers
{
    [Authorize(Roles = AppRoles.Administrator)]
    public class UserController : AppController
    {
        private readonly IUserRepository _userRepository;

        public UserController(IUserRepository userRepository) => _userRepository = userRepository;

        public async Task<IActionResult> Index() => View(await _userRepository.GetAllAsync());

        public IActionResult Create() => View("Form", new UserFormViewModel { Role = AppRoles.Manager });

        [HttpPost]
        public async Task<IActionResult> Create(UserFormViewModel vm)
        {
            if (string.IsNullOrWhiteSpace(vm.Password))
                ModelState.AddModelError(nameof(vm.Password), "Set a starting password");
            await ValidateCommonAsync(vm);
            if (!ModelState.IsValid) return View("Form", vm);

            await _userRepository.CreateAsync(new User
            {
                FullName = vm.FullName.Trim(),
                Email = vm.Email.Trim().ToLowerInvariant(),
                Role = vm.Role,
                PasswordHash = PasswordHelper.HashPassword(vm.Password!),
                IsActive = true
            });

            Toast($"{vm.FullName} can now sign in as {vm.Role}.");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return NotFound();

            return View("Form", new UserFormViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role
            });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(UserFormViewModel vm)
        {
            await ValidateCommonAsync(vm);
            if (vm.UserId == User.GetUserId() && vm.Role != AppRoles.Administrator)
                ModelState.AddModelError(nameof(vm.Role), "You can't remove your own administrator role");
            if (!ModelState.IsValid) return View("Form", vm);

            var user = await _userRepository.GetByIdAsync(vm.UserId);
            if (user == null) return NotFound();

            user.FullName = vm.FullName.Trim();
            user.Email = vm.Email.Trim().ToLowerInvariant();
            user.Role = vm.Role;
            await _userRepository.UpdateAsync(user);

            if (!string.IsNullOrWhiteSpace(vm.Password))
                await _userRepository.UpdatePasswordAsync(user.UserId, PasswordHelper.HashPassword(vm.Password));

            Toast("User saved.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            if (id == User.GetUserId())
            {
                Toast("You can't deactivate your own account.", "error");
                return RedirectToAction(nameof(Index));
            }

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return NotFound();

            await _userRepository.SetActiveAsync(id, !user.IsActive);
            Toast(user.IsActive ? $"{user.FullName} deactivated." : $"{user.FullName} reactivated.");
            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateCommonAsync(UserFormViewModel vm)
        {
            if (!AppRoles.All.Contains(vm.Role))
                ModelState.AddModelError(nameof(vm.Role), "Choose a valid role");
            if (!string.IsNullOrWhiteSpace(vm.Email) && await _userRepository.EmailExistsAsync(vm.Email, vm.UserId))
                ModelState.AddModelError(nameof(vm.Email), "Another user already has this email");
        }
    }
}
