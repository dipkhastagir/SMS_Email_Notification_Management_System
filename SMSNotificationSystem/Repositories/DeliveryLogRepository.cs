using Dapper;
using SMSNotificationSystem.Data;
using SMSNotificationSystem.DTOs;
using SMSNotificationSystem.Repositories.Interfaces;

namespace SMSNotificationSystem.Repositories
{
    public class DeliveryLogRepository : IDeliveryLogRepository
    {
        private readonly DapperContext _context;

        public DeliveryLogRepository(DapperContext context) => _context = context;

        public async Task<IEnumerable<DeliveryReportDto>> GetAllAsync(string? status = null, string? channel = null,
            string? search = null, DateTime? from = null, DateTime? to = null, int top = 500)
        {
            const string sql = @"SELECT TOP (@Top) *
                                 FROM dbo.vw_DeliveryReport
                                 WHERE (@Status IS NULL OR Status = @Status)
                                   AND (@Channel IS NULL OR Channel = @Channel)
                                   AND (@Search IS NULL OR RecipientName LIKE @Search OR RecipientContact LIKE @Search
                                        OR TriggerName LIKE @Search OR FailureReason LIKE @Search)
                                   AND (@From IS NULL OR AttemptedAt >= @From)
                                   AND (@To IS NULL OR AttemptedAt < DATEADD(DAY, 1, @To))
                                 ORDER BY AttemptedAt DESC, LogId DESC";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<DeliveryReportDto>(sql, new
            {
                Top = top,
                Status = string.IsNullOrWhiteSpace(status) ? null : status,
                Channel = string.IsNullOrWhiteSpace(channel) ? null : channel,
                Search = string.IsNullOrWhiteSpace(search) ? null : "%" + search.Trim() + "%",
                From = from?.Date,
                To = to?.Date
            });
        }

        public async Task<IEnumerable<DeliveryReportDto>> GetRecentAsync(int count)
        {
            const string sql = "SELECT TOP (@Count) * FROM dbo.vw_DeliveryReport ORDER BY AttemptedAt DESC, LogId DESC";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<DeliveryReportDto>(sql, new { Count = count });
        }

        public async Task<IEnumerable<DeliveryReportDto>> GetByMessageIdAsync(int messageId)
        {
            const string sql = "SELECT * FROM dbo.vw_DeliveryReport WHERE MessageId = @Id ORDER BY AttemptNo";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<DeliveryReportDto>(sql, new { Id = messageId });
        }
    }
}
