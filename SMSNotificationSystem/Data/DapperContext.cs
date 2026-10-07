using Microsoft.Data.SqlClient;
using System.Data;

namespace SMSNotificationSystem.Data
{
    /// <summary>Creates SQL Server connections for Dapper using "DefaultConnection" from appsettings.json.</summary>
    public class DapperContext
    {
        private readonly string _connectionString;

        public DapperContext(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing in appsettings.json.");
        }

        public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
    }
}
