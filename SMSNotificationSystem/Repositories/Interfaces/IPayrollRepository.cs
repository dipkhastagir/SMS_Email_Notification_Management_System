using SMSNotificationSystem.DTOs;

namespace SMSNotificationSystem.Repositories.Interfaces
{
    public interface IPayrollRepository
    {
        Task<IEnumerable<SalaryPaymentDto>> GetByMonthAsync(string salaryMonth);
        Task<IEnumerable<SalaryPaymentDto>> GetRecentAsync(int days);

        /// <summary>Calls sp_DisburseSalary. Returns PaymentId, -1 if already paid, 0 if employee inactive.</summary>
        Task<int> DisburseAsync(int employeeId, string salaryMonth, int? paidBy);
    }
}
