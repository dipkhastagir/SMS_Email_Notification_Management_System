using System.Data;
using Dapper;
using SMSNotificationSystem.Data;
using SMSNotificationSystem.DTOs;
using SMSNotificationSystem.Models;
using SMSNotificationSystem.Repositories.Interfaces;

namespace SMSNotificationSystem.Repositories
{
    public class QueueRepository : IQueueRepository
    {
        private readonly DapperContext _context;

        public QueueRepository(DapperContext context) => _context = context;

        public async Task<IEnumerable<QueueItemDto>> GetAllAsync(string? status = null, string? channel = null, int top = 300)
        {
            const string sql = @"SELECT TOP (@Top) *
                                 FROM dbo.vw_QueueDetails
                                 WHERE (@Status IS NULL OR Status = @Status)
                                   AND (@Channel IS NULL OR Channel = @Channel)
                                 ORDER BY CreatedAt DESC, MessageId DESC";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<QueueItemDto>(sql, new
            {
                Top = top,
                Status = string.IsNullOrWhiteSpace(status) ? null : status,
                Channel = string.IsNullOrWhiteSpace(channel) ? null : channel
            });
        }

        public async Task<IDictionary<string, int>> GetStatusCountsAsync()
        {
            const string sql = "SELECT Status, COUNT(*) AS Total FROM dbo.MessageQueue GROUP BY Status";
            using var connection = _context.CreateConnection();
            var rows = await connection.QueryAsync<(string Status, int Total)>(sql);
            return rows.ToDictionary(r => r.Status, r => r.Total);
        }

        public async Task<int> EnqueueAsync(MessageQueue message, int cooldownHours = 24, bool oneTime = false)
        {
            using var connection = _context.CreateConnection();
            return await connection.ExecuteScalarAsync<int>("dbo.sp_EnqueueMessage", new
            {
                message.TriggerId,
                message.TemplateId,
                message.ReferenceKey,
                message.RecipientName,
                message.RecipientContact,
                message.Channel,
                message.Subject,
                message.MessageBody,
                message.CreatedBy,
                CooldownHours = cooldownHours,
                OneTime = oneTime
            }, commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<MessageQueue>> ClaimPendingAsync(int batchSize)
        {
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<MessageQueue>("dbo.sp_ClaimPendingMessages",
                new { BatchSize = batchSize }, commandType: CommandType.StoredProcedure);
        }

        public async Task<string> RecordResultAsync(int messageId, GatewayResult result, int maxRetries)
        {
            using var connection = _context.CreateConnection();
            return await connection.ExecuteScalarAsync<string>("dbo.sp_RecordDeliveryResult", new
            {
                MessageId = messageId,
                result.IsSuccess,
                GatewayProvider = Truncate(result.Provider, 80),
                GatewayResponse = Truncate(result.Response, 500),
                FailureReason = result.FailureReason == null ? null : Truncate(result.FailureReason, 250),
                IsPermanent = result.IsPermanentFailure,
                MaxRetries = maxRetries
            }, commandType: CommandType.StoredProcedure) ?? "Failed";
        }

        public async Task<bool> RequeueAsync(int messageId)
        {
            const string sql = @"UPDATE dbo.MessageQueue SET Status = N'Pending', RetryCount = 0
                                 WHERE MessageId = @Id AND Status IN (N'Failed', N'Cancelled')";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(sql, new { Id = messageId }) > 0;
        }

        public async Task<bool> CancelAsync(int messageId)
        {
            const string sql = @"UPDATE dbo.MessageQueue SET Status = N'Cancelled'
                                 WHERE MessageId = @Id AND Status IN (N'Pending', N'Retrying')";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(sql, new { Id = messageId }) > 0;
        }

        private static string Truncate(string value, int max) =>
            string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];
    }
}
