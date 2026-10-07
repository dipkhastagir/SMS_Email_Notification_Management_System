using Dapper;
using SMSNotificationSystem.Data;
using SMSNotificationSystem.Models;
using SMSNotificationSystem.Repositories.Interfaces;

namespace SMSNotificationSystem.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly DapperContext _context;

        public UserRepository(DapperContext context) => _context = context;

        public async Task<User?> GetByEmailAsync(string email)
        {
            const string sql = "SELECT * FROM dbo.Users WHERE Email = @Email";
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email.Trim() });
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM dbo.Users WHERE UserId = @Id";
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            const string sql = "SELECT * FROM dbo.Users ORDER BY IsActive DESC, FullName";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<User>(sql);
        }

        public async Task<bool> EmailExistsAsync(string email, int excludeUserId = 0)
        {
            const string sql = "SELECT COUNT(1) FROM dbo.Users WHERE Email = @Email AND UserId <> @Id";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, new { Email = email.Trim(), Id = excludeUserId }) > 0;
        }

        public async Task<int> CreateAsync(User user)
        {
            const string sql = @"INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role, IsActive, CreatedAt)
                                 VALUES (@FullName, @Email, @PasswordHash, @Role, @IsActive, SYSDATETIME());
                                 SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, user);
        }

        public async Task<bool> UpdateAsync(User user)
        {
            const string sql = @"UPDATE dbo.Users SET FullName = @FullName, Email = @Email, Role = @Role
                                 WHERE UserId = @UserId";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(sql, user) > 0;
        }

        public async Task<bool> UpdatePasswordAsync(int userId, string passwordHash)
        {
            const string sql = "UPDATE dbo.Users SET PasswordHash = @Hash WHERE UserId = @Id";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(sql, new { Hash = passwordHash, Id = userId }) > 0;
        }

        public async Task<bool> SetActiveAsync(int userId, bool isActive)
        {
            const string sql = "UPDATE dbo.Users SET IsActive = @IsActive WHERE UserId = @Id";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(sql, new { IsActive = isActive, Id = userId }) > 0;
        }

        public async Task UpdateLastLoginAsync(int userId)
        {
            const string sql = "UPDATE dbo.Users SET LastLoginAt = SYSDATETIME() WHERE UserId = @Id";
            using var connection = _context.CreateConnection();
            await connection.ExecuteAsync(sql, new { Id = userId });
        }
    }
}
