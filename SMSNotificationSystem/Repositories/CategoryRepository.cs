using Dapper;
using SMSNotificationSystem.Data;
using SMSNotificationSystem.Models;
using SMSNotificationSystem.Repositories.Interfaces;

namespace SMSNotificationSystem.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly DapperContext _context;

        public CategoryRepository(DapperContext context) => _context = context;

        public async Task<IEnumerable<TemplateCategory>> GetAllAsync()
        {
            const string sql = @"SELECT c.CategoryId, c.Name, c.Description,
                                        (SELECT COUNT(*) FROM dbo.MessageTemplates t WHERE t.CategoryId = c.CategoryId) AS TemplateCount
                                 FROM dbo.TemplateCategories c
                                 ORDER BY c.Name";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<TemplateCategory>(sql);
        }

        public async Task<bool> NameExistsAsync(string name)
        {
            const string sql = "SELECT COUNT(1) FROM dbo.TemplateCategories WHERE Name = @Name";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, new { Name = name.Trim() }) > 0;
        }

        public async Task<int> CreateAsync(TemplateCategory category)
        {
            const string sql = @"INSERT INTO dbo.TemplateCategories (Name, Description) VALUES (@Name, @Description);
                                 SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, category);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            // Only deletes a category that has no templates
            const string sql = @"DELETE FROM dbo.TemplateCategories
                                 WHERE CategoryId = @Id
                                   AND NOT EXISTS (SELECT 1 FROM dbo.MessageTemplates WHERE CategoryId = @Id)";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(sql, new { Id = id }) > 0;
        }
    }
}
