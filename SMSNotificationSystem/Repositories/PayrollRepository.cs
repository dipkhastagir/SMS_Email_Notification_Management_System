using System.Data;
using Dapper;
using SMSNotificationSystem.Data;
using SMSNotificationSystem.DTOs;
using SMSNotificationSystem.Repositories.Interfaces;

namespace SMSNotificationSystem.Repositories
{
    public class PayrollRepository : IPayrollRepository
    {
        private readonly DapperContext _context;

        public PayrollRepository(DapperContext context) => _context = context;

        public async Task<IEnumerable<SalaryPaymentDto>> GetByMonthAsync(string salaryMonth)
        {
            const string sql = "SELECT * FROM dbo.vw_SalaryPayments WHERE SalaryMonth = @Month ORDER BY EmployeeName";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<SalaryPaymentDto>(sql, new { Month = salaryMonth });
        }

        public async Task<IEnumerable<SalaryPaymentDto>> GetRecentAsync(int days)
        {
            const string sql = @"SELECT * FROM dbo.vw_SalaryPayments
                                 WHERE PaidAt >= DATEADD(DAY, -@Days, SYSDATETIME())
                                 ORDER BY PaidAt DESC";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<SalaryPaymentDto>(sql, new { Days = days });
        }

        public async Task<int> DisburseAsync(int employeeId, string salaryMonth, int? paidBy)
        {
            using var connection = _context.CreateConnection();
            return await connection.ExecuteScalarAsync<int>("dbo.sp_DisburseSalary",
                new { EmployeeId = employeeId, SalaryMonth = salaryMonth, PaidBy = paidBy },
                commandType: CommandType.StoredProcedure);
        }
    }
}
