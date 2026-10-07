using SMSNotificationSystem.DTOs;
using SMSNotificationSystem.Models;

namespace SMSNotificationSystem.ViewModels
{
    public class PayrollViewModel
    {
        public string Month { get; set; } = DateTime.Today.ToString("yyyy-MM");
        public List<Employee> Employees { get; set; } = new();
        public Dictionary<int, SalaryPaymentDto> Payments { get; set; } = new();

        public decimal TotalPaid => Payments.Values.Sum(p => p.Amount);
        public decimal TotalDue => Employees.Where(e => !Payments.ContainsKey(e.EmployeeId)).Sum(e => e.Salary);

        public string MonthLabel =>
            DateTime.TryParse(Month + "-01", out var d) ? d.ToString("MMMM yyyy") : Month;
    }
}
