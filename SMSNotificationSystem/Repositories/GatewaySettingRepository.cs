using Dapper;
using SMSNotificationSystem.Data;
using SMSNotificationSystem.Models;
using SMSNotificationSystem.Repositories.Interfaces;

namespace SMSNotificationSystem.Repositories
{
    public class GatewaySettingRepository : IGatewaySettingRepository
    {
        private readonly DapperContext _context;

        public GatewaySettingRepository(DapperContext context) => _context = context;

        public async Task<IEnumerable<GatewaySetting>> GetAllAsync()
        {
            const string sql = "SELECT * FROM dbo.GatewaySettings ORDER BY Channel, IsPrimary DESC, Provider";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<GatewaySetting>(sql);
        }

        public async Task<GatewaySetting?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM dbo.GatewaySettings WHERE GatewayId = @Id";
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<GatewaySetting>(sql, new { Id = id });
        }

        public async Task<GatewaySetting?> GetPrimaryActiveAsync(string channel)
        {
            // Prefer the primary gateway; fall back to any active one for the channel
            const string sql = @"SELECT TOP 1 * FROM dbo.GatewaySettings
                                 WHERE Channel = @Channel AND IsActive = 1
                                 ORDER BY IsPrimary DESC, GatewayId";
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<GatewaySetting>(sql, new { Channel = channel });
        }

        public async Task<int> CreateAsync(GatewaySetting setting)
        {
            const string sql = @"IF @IsPrimary = 1
                                     UPDATE dbo.GatewaySettings SET IsPrimary = 0 WHERE Channel = @Channel;
                                 INSERT INTO dbo.GatewaySettings (Provider, Channel, ApiKey, SenderId, IsPrimary, IsActive, CreatedAt)
                                 VALUES (@Provider, @Channel, @ApiKey, @SenderId, @IsPrimary, @IsActive, SYSDATETIME());
                                 SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var connection = _context.CreateConnection();
            connection.Open();
            using var tx = connection.BeginTransaction();
            var id = await connection.ExecuteScalarAsync<int>(sql, setting, tx);
            tx.Commit();
            return id;
        }

        public async Task<bool> UpdateAsync(GatewaySetting setting)
        {
            const string sql = @"IF @IsPrimary = 1
                                     UPDATE dbo.GatewaySettings SET IsPrimary = 0 WHERE Channel = @Channel AND GatewayId <> @GatewayId;
                                 UPDATE dbo.GatewaySettings
                                 SET Provider = @Provider, Channel = @Channel, ApiKey = @ApiKey, SenderId = @SenderId,
                                     IsPrimary = @IsPrimary, IsActive = @IsActive
                                 WHERE GatewayId = @GatewayId;";
            using var connection = _context.CreateConnection();
            connection.Open();
            using var tx = connection.BeginTransaction();
            var rows = await connection.ExecuteAsync(sql, setting, tx);
            tx.Commit();
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM dbo.GatewaySettings WHERE GatewayId = @Id";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(sql, new { Id = id }) > 0;
        }
    }
}
