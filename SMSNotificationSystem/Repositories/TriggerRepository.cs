using Dapper;
using SMSNotificationSystem.Data;
using SMSNotificationSystem.DTOs;
using SMSNotificationSystem.Models;
using SMSNotificationSystem.Repositories.Interfaces;

namespace SMSNotificationSystem.Repositories
{
    public class TriggerRepository : ITriggerRepository
    {
        private readonly DapperContext _context;

        public TriggerRepository(DapperContext context) => _context = context;

        public async Task<IEnumerable<TriggerWithTemplateDto>> GetAllAsync()
        {
            const string sql = "SELECT * FROM dbo.vw_TriggerDetails ORDER BY IsActive DESC, Name";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<TriggerWithTemplateDto>(sql);
        }

        public async Task<IEnumerable<EventTrigger>> GetActiveAsync()
        {
            const string sql = "SELECT * FROM dbo.EventTriggers WHERE IsActive = 1 ORDER BY TriggerId";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<EventTrigger>(sql);
        }

        public async Task<EventTrigger?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM dbo.EventTriggers WHERE TriggerId = @Id";
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<EventTrigger>(sql, new { Id = id });
        }

        public async Task<int> CreateAsync(EventTrigger trigger)
        {
            const string sql = @"INSERT INTO dbo.EventTriggers
                                    (Name, EventType, TemplateId, ConditionField, ConditionOperator, ConditionValue,
                                     RecipientName, RecipientContact, CooldownHours, IsActive, CreatedAt)
                                 VALUES
                                    (@Name, @EventType, @TemplateId, @ConditionField, @ConditionOperator, @ConditionValue,
                                     @RecipientName, @RecipientContact, @CooldownHours, @IsActive, SYSDATETIME());
                                 SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, trigger);
        }

        public async Task<bool> UpdateAsync(EventTrigger trigger)
        {
            const string sql = @"UPDATE dbo.EventTriggers
                                 SET Name = @Name, EventType = @EventType, TemplateId = @TemplateId,
                                     ConditionField = @ConditionField, ConditionOperator = @ConditionOperator,
                                     ConditionValue = @ConditionValue, RecipientName = @RecipientName,
                                     RecipientContact = @RecipientContact, CooldownHours = @CooldownHours, IsActive = @IsActive
                                 WHERE TriggerId = @TriggerId";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(sql, trigger) > 0;
        }

        public async Task<bool> SetActiveAsync(int id, bool isActive)
        {
            const string sql = "UPDATE dbo.EventTriggers SET IsActive = @IsActive WHERE TriggerId = @Id";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(sql, new { Id = id, IsActive = isActive }) > 0;
        }

        public async Task UpdateLastRunAsync(int id)
        {
            const string sql = "UPDATE dbo.EventTriggers SET LastRunAt = SYSDATETIME() WHERE TriggerId = @Id";
            using var connection = _context.CreateConnection();
            await connection.ExecuteAsync(sql, new { Id = id });
        }

        public async Task<bool> DeleteAsync(int id)
        {
            // Queued messages keep their history: FK_MessageQueue_Trigger is ON DELETE SET NULL
            const string sql = "DELETE FROM dbo.EventTriggers WHERE TriggerId = @Id";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(sql, new { Id = id }) > 0;
        }
    }
}
