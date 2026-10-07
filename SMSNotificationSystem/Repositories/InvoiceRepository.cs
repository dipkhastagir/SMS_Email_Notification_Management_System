using Dapper;
using SMSNotificationSystem.Data;
using SMSNotificationSystem.Models;
using SMSNotificationSystem.Repositories.Interfaces;

namespace SMSNotificationSystem.Repositories
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly DapperContext _context;

        public InvoiceRepository(DapperContext context) => _context = context;

        public async Task<IEnumerable<Invoice>> GetAllAsync()
        {
            const string sql = "SELECT * FROM dbo.Invoices ORDER BY IsPaid, DueDate";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<Invoice>(sql);
        }

        public async Task<IEnumerable<Invoice>> GetUnpaidAsync()
        {
            const string sql = "SELECT * FROM dbo.Invoices WHERE IsPaid = 0 ORDER BY DueDate";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<Invoice>(sql);
        }

        public async Task<Invoice?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM dbo.Invoices WHERE InvoiceId = @Id";
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Invoice>(sql, new { Id = id });
        }

        public async Task<bool> InvoiceNoExistsAsync(string invoiceNo, int excludeId = 0)
        {
            const string sql = "SELECT COUNT(1) FROM dbo.Invoices WHERE InvoiceNo = @No AND InvoiceId <> @Id";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, new { No = invoiceNo.Trim(), Id = excludeId }) > 0;
        }

        public async Task<string> GetNextInvoiceNoAsync()
        {
            const string sql = "SELECT ISNULL(MAX(InvoiceId), 0) + 1 FROM dbo.Invoices";
            using var connection = _context.CreateConnection();
            var next = await connection.ExecuteScalarAsync<int>(sql);
            return $"INV-{DateTime.Today:yyyy}-{next:0000}";
        }

        public async Task<int> CreateAsync(Invoice invoice)
        {
            const string sql = @"INSERT INTO dbo.Invoices (InvoiceNo, CustomerName, CustomerPhone, CustomerEmail, Amount, DueDate, IsPaid, CreatedAt)
                                 VALUES (@InvoiceNo, @CustomerName, @CustomerPhone, @CustomerEmail, @Amount, @DueDate, 0, SYSDATETIME());
                                 SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, invoice);
        }

        public async Task<bool> UpdateAsync(Invoice invoice)
        {
            const string sql = @"UPDATE dbo.Invoices
                                 SET InvoiceNo = @InvoiceNo, CustomerName = @CustomerName, CustomerPhone = @CustomerPhone,
                                     CustomerEmail = @CustomerEmail, Amount = @Amount, DueDate = @DueDate
                                 WHERE InvoiceId = @InvoiceId";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(sql, invoice) > 0;
        }

        public async Task<bool> MarkAsPaidAsync(int invoiceId)
        {
            const string sql = "UPDATE dbo.Invoices SET IsPaid = 1, PaidAt = SYSDATETIME() WHERE InvoiceId = @Id AND IsPaid = 0";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(sql, new { Id = invoiceId }) > 0;
        }

        public async Task<bool> DeleteAsync(int invoiceId)
        {
            const string sql = "DELETE FROM dbo.Invoices WHERE InvoiceId = @Id";
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(sql, new { Id = invoiceId }) > 0;
        }
    }
}
