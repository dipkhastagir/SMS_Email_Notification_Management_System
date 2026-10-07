using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMSNotificationSystem.Helpers;
using SMSNotificationSystem.Repositories.Interfaces;
using SMSNotificationSystem.ViewModels;

namespace SMSNotificationSystem.Controllers
{
    public class AccountController : AppController
    {
        private readonly IUserRepository _userRepository;

        public AccountController(IUserRepository userRepository) => _userRepository = userRepository;

        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _userRepository.GetByEmailAsync(vm.Email);
            if (user == null || !PasswordHelper.VerifyPassword(vm.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "That email and password don't match an account.");
                return View(vm);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "This account is deactivated. Ask an administrator to reactivate it.");
                return View(vm);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = vm.RememberMe });

            await _userRepository.UpdateLastLoginAsync(user.UserId);

            if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
                return Redirect(vm.ReturnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [Authorize]
        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var userId = User.GetUserId();
            var user = userId == null ? null : await _userRepository.GetByIdAsync(userId.Value);
            if (user == null) return RedirectToAction(nameof(Login));

            if (!PasswordHelper.VerifyPassword(vm.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError(nameof(vm.CurrentPassword), "The current password is not correct.");
                return View(vm);
            }

            await _userRepository.UpdatePasswordAsync(user.UserId, PasswordHelper.HashPassword(vm.NewPassword));
            Toast("Password changed.");
            return RedirectToAction("Index", "Dashboard");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied() => View();
    }
}
