using System.Data;
using Dapper;
using SMSNotificationSystem.Data;
using SMSNotificationSystem.DTOs;
using SMSNotificationSystem.Repositories.Interfaces;

namespace SMSNotificationSystem.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly DapperContext _context;

        public DashboardRepository(DapperContext context) => _context = context;

        public async Task<DashboardStatsDto> GetStatsAsync()
        {
            using var connection = _context.CreateConnection();
            return await connection.QuerySingleAsync<DashboardStatsDto>("SELECT * FROM dbo.vw_DashboardStats");
        }

        public async Task<IEnumerable<DailyDeliveryDto>> GetDailyStatsAsync(int days)
        {
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<DailyDeliveryDto>("dbo.sp_GetDailyDeliveryStats",
                new { Days = days }, commandType: CommandType.StoredProcedure);
        }
    }
}
