using Dapper;
using SMSNotificationSystem.Data;
using SMSNotificationSystem.Models;
using SMSNotificationSystem.Repositories.Interfaces;

namespace SMSNotificationSystem.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly DapperContext _context;

        public EmployeeRepository(DapperContext context) => _context = context;

        public async Task<IEnumerable<Employee>> GetAllAsync(bool includeInactive = true)
        {
            const string sql = @"SELECT * FROM dbo.Employees
                                 WHERE (@IncludeInactive = 1 OR IsActive = 1)
                                 ORDER BY IsActive DESC, FullName";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<Employee>(sql, new { IncludeInactive = includeInactive });
        }

        public async Task<Employee?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM dbo.Employees WHERE EmployeeId = @Id";
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Employee>(sql, new { Id = id });
        }

        public async Task<int> CreateAsync(Employee employee)
        {
            const string sql = @"INSERT INTO dbo.Employees (FullName, Phone, Email, Department, Designation, Salary, IsActive, JoinedAt)
                                 VALUES (@FullName, @Phone, @Email, @Department, @Designation, @Salary, @IsActive, @JoinedAt);
                                 SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, employee);
        }

        public async Task<bool> UpdateAsync(Employee employee)
        {
            const string sql = @"UPDATE dbo.Employees
                                 SET FullName = @FullName, Phone = @Phone, Email = @Email, Department = @Department,
                                     Designation = @Designation, Salary = @Salary, IsActive = @IsActive, JoinedAt = @JoinedAt
                                 WHERE EmployeeId = @EmployeeId";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(sql, employee) > 0;
        }

        public async Task<bool> SetActiveAsync(int id, bool isActive)
        {
            const string sql = "UPDATE dbo.Employees SET IsActive = @IsActive WHERE EmployeeId = @Id";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(sql, new { Id = id, IsActive = isActive }) > 0;
        }
    }
}
