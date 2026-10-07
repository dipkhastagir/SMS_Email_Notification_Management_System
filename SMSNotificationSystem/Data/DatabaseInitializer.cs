using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace SMSNotificationSystem.Data
{
    /// <summary>
    /// Runs the scripts in the SQL folder (embedded in the app) at startup, so the database,
    /// tables, views, stored procedures and demo data always exist before anything uses them.
    /// Every script is safe to re-run: it only creates what is missing.
    /// The same scripts can still be run by hand in SQL Server Management Studio.
    /// </summary>
    public static partial class DatabaseInitializer
    {
        private static readonly string[] Scripts =
        {
            "01_CreateDatabase.sql",
            "02_CreateTables.sql",
            "03_CreateViews.sql",
            "04_CreateStoredProcedures.sql",
            "05_SeedData.sql"
        };

        [GeneratedRegex(@"^\s*GO\s*;?\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
        private static partial Regex GoSeparator();

        public static void EnsureDatabase(string connectionString, bool recreateIfOutdated, ILogger logger)
        {
            var target = new SqlConnectionStringBuilder(connectionString);
            var databaseName = string.IsNullOrWhiteSpace(target.InitialCatalog) ? "SmsNotificationDB" : target.InitialCatalog;
            var master = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };

            try
            {
                using var connection = new SqlConnection(master.ConnectionString);
                connection.Open();

                if (IsOutdated(connection, databaseName))
                {
                    if (!recreateIfOutdated)
                    {
                        throw new InvalidOperationException(
                            $"The database '{databaseName}' was created by an older version of this project and has different tables. " +
                            "Run SQL/99_ResetDatabase.sql in SSMS (or set \"Database:RecreateIfOutdated\": true in appsettings) and start the app again.");
                    }

                    logger.LogWarning("Database {Db} has the old table layout. Dropping and recreating it.", databaseName);
                    Execute(connection, $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}];");
                }

                foreach (var script in Scripts)
                {
                    foreach (var batch in GoSeparator().Split(ReadScript(script)))
                    {
                        if (!string.IsNullOrWhiteSpace(batch)) Execute(connection, batch);
                    }
                }

                logger.LogInformation("Database {Db} is ready (tables, views, stored procedures and seed data checked).", databaseName);
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException(
                    $"Could not prepare the SQL Server database '{databaseName}' on server '{target.DataSource}'. " +
                    "Check that SQL Server is running and that 'Server=' in appsettings.json matches the server name you use in SSMS " +
                    $"(for example '.', 'localhost' or '.\\SQLEXPRESS'). SQL Server said: {ex.Message}", ex);
            }
        }

        /// <summary>True when the database exists but still has the tables of the previous version.</summary>
        private static bool IsOutdated(SqlConnection connection, string databaseName)
        {
            using var exists = new SqlCommand("SELECT DB_ID(@Db)", connection);
            exists.Parameters.AddWithValue("@Db", databaseName);
            if (exists.ExecuteScalar() is DBNull or null) return false;

            var sql = $@"
                SELECT CASE WHEN
                    (OBJECT_ID(N'[{databaseName}].dbo.EventTriggers', N'U') IS NOT NULL AND COL_LENGTH(N'[{databaseName}].dbo.EventTriggers', N'Name') IS NULL)
                 OR (OBJECT_ID(N'[{databaseName}].dbo.MessageQueue', N'U') IS NOT NULL AND COL_LENGTH(N'[{databaseName}].dbo.MessageQueue', N'ReferenceKey') IS NULL)
                 OR (OBJECT_ID(N'[{databaseName}].dbo.GatewaySettings', N'U') IS NOT NULL AND COL_LENGTH(N'[{databaseName}].dbo.GatewaySettings', N'Channel') IS NULL)
                 OR (OBJECT_ID(N'[{databaseName}].dbo.Invoices', N'U') IS NOT NULL AND COL_LENGTH(N'[{databaseName}].dbo.Invoices', N'InvoiceNo') IS NULL)
                 OR (OBJECT_ID(N'[{databaseName}].dbo.Employees', N'U') IS NOT NULL AND COL_LENGTH(N'[{databaseName}].dbo.Employees', N'Phone') IS NULL)
                THEN 1 ELSE 0 END";
            using var check = new SqlCommand(sql, connection);
            return Convert.ToInt32(check.ExecuteScalar()) == 1;
        }

        private static void Execute(SqlConnection connection, string sql)
        {
            using var command = new SqlCommand(sql, connection) { CommandTimeout = 180 };
            command.ExecuteNonQuery();
        }

        private static string ReadScript(string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(fileName)
                ?? throw new InvalidOperationException($"SQL script '{fileName}' is not embedded in the application.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
