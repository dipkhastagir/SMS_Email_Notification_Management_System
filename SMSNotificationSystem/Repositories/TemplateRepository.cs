using Dapper;
using SMSNotificationSystem.Data;
using SMSNotificationSystem.DTOs;
using SMSNotificationSystem.Models;
using SMSNotificationSystem.Repositories.Interfaces;

namespace SMSNotificationSystem.Repositories
{
    public class TemplateRepository : ITemplateRepository
    {
        private readonly DapperContext _context;

        public TemplateRepository(DapperContext context) => _context = context;

        public async Task<IEnumerable<TemplateWithCategoryDto>> GetAllAsync()
        {
            const string sql = "SELECT * FROM dbo.vw_TemplateDetails ORDER BY Status, CategoryName, Name";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<TemplateWithCategoryDto>(sql);
        }

        public async Task<IEnumerable<MessageTemplate>> GetActiveAsync()
        {
            const string sql = "SELECT * FROM dbo.MessageTemplates WHERE Status = N'Active' ORDER BY Name";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<MessageTemplate>(sql);
        }

        public async Task<MessageTemplate?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM dbo.MessageTemplates WHERE TemplateId = @Id";
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<MessageTemplate>(sql, new { Id = id });
        }

        public async Task<int> CreateAsync(MessageTemplate template)
        {
            const string sql = @"INSERT INTO dbo.MessageTemplates (Name, CategoryId, Channel, Subject, Body, Status, CreatedBy, CreatedAt)
                                 VALUES (@Name, @CategoryId, @Channel, @Subject, @Body, @Status, @CreatedBy, SYSDATETIME());
                                 SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, template);
        }

        public async Task<bool> UpdateAsync(MessageTemplate template)
        {
            const string sql = @"UPDATE dbo.MessageTemplates
                                 SET Name = @Name, CategoryId = @CategoryId, Channel = @Channel, Subject = @Subject,
                                     Body = @Body, Status = @Status, UpdatedAt = SYSDATETIME()
                                 WHERE TemplateId = @TemplateId";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(sql, template) > 0;
        }

        public async Task<bool> SetStatusAsync(int id, string status)
        {
            const string sql = "UPDATE dbo.MessageTemplates SET Status = @Status, UpdatedAt = SYSDATETIME() WHERE TemplateId = @Id";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(sql, new { Id = id, Status = status }) > 0;
        }

        public async Task<bool> IsUsedByTriggerAsync(int id)
        {
            const string sql = "SELECT COUNT(1) FROM dbo.EventTriggers WHERE TemplateId = @Id";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, new { Id = id }) > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM dbo.MessageTemplates WHERE TemplateId = @Id";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(sql, new { Id = id }) > 0;
        }
    }
}
