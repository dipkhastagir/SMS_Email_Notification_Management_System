using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMSNotificationSystem.Helpers;
using SMSNotificationSystem.Models;
using SMSNotificationSystem.Repositories.Interfaces;
using SMSNotificationSystem.ViewModels;

namespace SMSNotificationSystem.Controllers
{
    [Authorize(Roles = AppRoles.AdminOrHR)]
    public class AttendanceController : AppController
    {
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IEmployeeRepository _employeeRepository;

        public AttendanceController(IAttendanceRepository attendanceRepository, IEmployeeRepository employeeRepository)
        {
            _attendanceRepository = attendanceRepository;
            _employeeRepository = employeeRepository;
        }

        /// <summary>Daily attendance sheet: every active employee with today's (or the chosen day's) status.</summary>
        public async Task<IActionResult> Index(DateTime? date = null)
        {
            var day = (date ?? DateTime.Today).Date;
            if (day > DateTime.Today) day = DateTime.Today;

            var employees = await _employeeRepository.GetAllAsync(includeInactive: false);
            var recorded = (await _attendanceRepository.GetByDateAsync(day)).ToDictionary(a => a.EmployeeId);

            var vm = new AttendanceSheetViewModel
            {
                Date = day,
                Rows = employees.Select(e => new AttendanceSheetRow
                {
                    EmployeeId = e.EmployeeId,
                    EmployeeName = e.FullName,
                    Department = e.Department,
                    Status = recorded.TryGetValue(e.EmployeeId, out var a) ? a.Status : "Present",
                    Remarks = recorded.TryGetValue(e.EmployeeId, out var r) ? r.Remarks : null,
                    IsRecorded = recorded.ContainsKey(e.EmployeeId)
                }).ToList(),
                RecentExceptions = (await _attendanceRepository.GetRecentAsync(7)).ToList()
            };
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Save(AttendanceSheetViewModel vm)
        {
            var day = vm.Date.Date;
            if (day > DateTime.Today)
            {
                Toast("Attendance can't be recorded for a future date.", "error");
                return RedirectToAction(nameof(Index));
            }

            var saved = 0;
            foreach (var row in vm.Rows)
            {
                if (!Attendance.Statuses.Contains(row.Status)) continue;
                await _attendanceRepository.UpsertAsync(row.EmployeeId, day, row.Status, row.Remarks);
                saved++;
            }

            var absent = vm.Rows.Count(r => r.Status == "Absent");
            Toast(absent > 0 && day == DateTime.Today
                ? $"Attendance saved for {saved} employees. {absent} absence alert(s) will be queued on the next scan."
                : $"Attendance saved for {saved} employees.");
            return RedirectToAction(nameof(Index), new { date = day.ToString("yyyy-MM-dd") });
        }
    }
}
