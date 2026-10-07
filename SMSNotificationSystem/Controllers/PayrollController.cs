using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMSNotificationSystem.Helpers;
using SMSNotificationSystem.Repositories.Interfaces;
using SMSNotificationSystem.ViewModels;

namespace SMSNotificationSystem.Controllers
{
    [Authorize(Roles = AppRoles.Payroll)]
    public class PayrollController : AppController
    {
        private readonly IPayrollRepository _payrollRepository;
        private readonly IEmployeeRepository _employeeRepository;

        public PayrollController(IPayrollRepository payrollRepository, IEmployeeRepository employeeRepository)
        {
            _payrollRepository = payrollRepository;
            _employeeRepository = employeeRepository;
        }

        public async Task<IActionResult> Index(string? month = null)
        {
            month = NormalizeMonth(month);
            var vm = new PayrollViewModel
            {
                Month = month,
                Employees = (await _employeeRepository.GetAllAsync(includeInactive: false)).ToList(),
                Payments = (await _payrollRepository.GetByMonthAsync(month)).ToDictionary(p => p.EmployeeId)
            };
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Disburse(int employeeId, string month)
        {
            month = NormalizeMonth(month);
            var result = await _payrollRepository.DisburseAsync(employeeId, month, User.GetUserId());
            Toast(result switch
            {
                > 0 => "Salary disbursed. The employee gets a confirmation on the next trigger scan.",
                -1 => "This employee's salary for the month is already disbursed.",
                _ => "This employee is inactive, so no salary was recorded."
            }, result > 0 ? "success" : "error");
            return RedirectToAction(nameof(Index), new { month });
        }

        [HttpPost]
        public async Task<IActionResult> DisburseAll(string month)
        {
            month = NormalizeMonth(month);
            var employees = await _employeeRepository.GetAllAsync(includeInactive: false);
            var paid = 0;
            foreach (var e in employees)
            {
                if (await _payrollRepository.DisburseAsync(e.EmployeeId, month, User.GetUserId()) > 0) paid++;
            }
            Toast(paid > 0 ? $"Salary disbursed to {paid} employee(s)." : "Everyone is already paid for this month.", paid > 0 ? "success" : "info");
            return RedirectToAction(nameof(Index), new { month });
        }

        private static string NormalizeMonth(string? month) =>
            DateTime.TryParseExact((month ?? string.Empty) + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var d)
                ? d.ToString("yyyy-MM", CultureInfo.InvariantCulture)
                : DateTime.Today.ToString("yyyy-MM", CultureInfo.InvariantCulture);
    }
}
