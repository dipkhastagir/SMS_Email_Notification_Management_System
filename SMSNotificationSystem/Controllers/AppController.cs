using Microsoft.AspNetCore.Mvc;

namespace SMSNotificationSystem.Controllers
{
    /// <summary>Base controller with a helper for the floating toast notifications in _Layout.</summary>
    public abstract class AppController : Controller
    {
        protected void Toast(string message, string type = "success")
        {
            TempData["ToastMessage"] = message;
            TempData["ToastType"] = type;   // success | error | info
        }
    }
}
