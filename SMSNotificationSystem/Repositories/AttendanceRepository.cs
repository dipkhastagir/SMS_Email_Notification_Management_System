using System.Data;
using Dapper;
using SMSNotificationSystem.Data;
using SMSNotificationSystem.DTOs;
using SMSNotificationSystem.Repositories.Interfaces;

namespace SMSNotificationSystem.Repositories
{
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly DapperContext _context;

        public AttendanceRepository(DapperContext context) => _context = context;

        public async Task<IEnumerable<AttendanceWithEmployeeDto>> GetByDateAsync(DateTime date)
        {
            const string sql = @"SELECT * FROM dbo.vw_AttendanceDetails
                                 WHERE AttendanceDate = @Date
                                 ORDER BY EmployeeName";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<AttendanceWithEmployeeDto>(sql, new { Date = date.Date });
        }

        public async Task<IEnumerable<AttendanceWithEmployeeDto>> GetRecentAsync(int days)
        {
            const string sql = @"SELECT * FROM dbo.vw_AttendanceDetails
                                 WHERE AttendanceDate >= DATEADD(DAY, -@Days, CAST(SYSDATETIME() AS DATE))
                                   AND Status <> N'Present'
                                 ORDER BY AttendanceDate DESC, EmployeeName";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<AttendanceWithEmployeeDto>(sql, new { Days = days });
        }

        public async Task UpsertAsync(int employeeId, DateTime date, string status, string? remarks)
        {
            using var connection = _context.CreateConnection();
            await connection.ExecuteAsync("dbo.sp_UpsertAttendance", new
            {
                EmployeeId = employeeId,
                AttendanceDate = date.Date,
                Status = status,
                Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim()
            }, commandType: CommandType.StoredProcedure);
        }
    }
}
