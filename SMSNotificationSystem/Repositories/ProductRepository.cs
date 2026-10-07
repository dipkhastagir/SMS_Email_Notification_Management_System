using Dapper;
using SMSNotificationSystem.Data;
using SMSNotificationSystem.Models;
using SMSNotificationSystem.Repositories.Interfaces;

namespace SMSNotificationSystem.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly DapperContext _context;

        public ProductRepository(DapperContext context) => _context = context;

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            const string sql = @"SELECT * FROM dbo.Products
                                 ORDER BY CASE WHEN StockQty <= ReorderLevel THEN 0 ELSE 1 END, ProductName";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<Product>(sql);
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM dbo.Products WHERE ProductId = @Id";
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Product>(sql, new { Id = id });
        }

        public async Task<int> CreateAsync(Product product)
        {
            const string sql = @"INSERT INTO dbo.Products (ProductName, Sku, StockQty, ReorderLevel, UnitPrice, UpdatedAt)
                                 VALUES (@ProductName, @Sku, @StockQty, @ReorderLevel, @UnitPrice, SYSDATETIME());
                                 SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, product);
        }

        public async Task<bool> UpdateAsync(Product product)
        {
            const string sql = @"UPDATE dbo.Products
                                 SET ProductName = @ProductName, Sku = @Sku, StockQty = @StockQty,
                                     ReorderLevel = @ReorderLevel, UnitPrice = @UnitPrice, UpdatedAt = SYSDATETIME()
                                 WHERE ProductId = @ProductId";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(sql, product) > 0;
        }

        public async Task<bool> AdjustStockAsync(int productId, int delta)
        {
            const string sql = @"UPDATE dbo.Products
                                 SET StockQty = CASE WHEN StockQty + @Delta < 0 THEN 0 ELSE StockQty + @Delta END,
                                     UpdatedAt = SYSDATETIME()
                                 WHERE ProductId = @Id";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(sql, new { Id = productId, Delta = delta }) > 0;
        }

        public async Task<bool> DeleteAsync(int productId)
        {
            const string sql = "DELETE FROM dbo.Products WHERE ProductId = @Id";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(sql, new { Id = productId }) > 0;
        }
    }
}
