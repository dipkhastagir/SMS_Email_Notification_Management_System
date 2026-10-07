using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMSNotificationSystem.Helpers;
using SMSNotificationSystem.Models;
using SMSNotificationSystem.Repositories.Interfaces;

namespace SMSNotificationSystem.Controllers
{
    [Authorize(Roles = AppRoles.AdminOrHR)]
    public class EmployeeController : AppController
    {
        private readonly IEmployeeRepository _employeeRepository;

        public EmployeeController(IEmployeeRepository employeeRepository) => _employeeRepository = employeeRepository;

        public async Task<IActionResult> Index() => View(await _employeeRepository.GetAllAsync());

        public IActionResult Create() => View("Form", new Employee());

        [HttpPost]
        public async Task<IActionResult> Create(Employee employee)
        {
            if (!ModelState.IsValid) return View("Form", employee);
            await _employeeRepository.CreateAsync(Clean(employee));
            Toast($"{employee.FullName} added.");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _employeeRepository.GetByIdAsync(id);
            return employee == null ? NotFound() : View("Form", employee);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Employee employee)
        {
            if (!ModelState.IsValid) return View("Form", employee);
            await _employeeRepository.UpdateAsync(Clean(employee));
            Toast("Employee saved.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var employee = await _employeeRepository.GetByIdAsync(id);
            if (employee == null) return NotFound();

            await _employeeRepository.SetActiveAsync(id, !employee.IsActive);
            Toast(employee.IsActive
                ? $"{employee.FullName} marked inactive. They won't appear on attendance or payroll."
                : $"{employee.FullName} is active again.");
            return RedirectToAction(nameof(Index));
        }

        private static Employee Clean(Employee e)
        {
            e.FullName = e.FullName.Trim();
            e.Phone = e.Phone.Trim();
            e.Email = string.IsNullOrWhiteSpace(e.Email) ? null : e.Email.Trim();
            e.Department = e.Department.Trim();
            e.Designation = string.IsNullOrWhiteSpace(e.Designation) ? null : e.Designation.Trim();
            return e;
        }
    }
}
