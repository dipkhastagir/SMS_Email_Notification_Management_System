using SMSNotificationSystem.Models;

namespace SMSNotificationSystem.Repositories.Interfaces
{
    public interface IInvoiceRepository
    {
        Task<IEnumerable<Invoice>> GetAllAsync();
        Task<IEnumerable<Invoice>> GetUnpaidAsync();
        Task<Invoice?> GetByIdAsync(int id);
        Task<bool> InvoiceNoExistsAsync(string invoiceNo, int excludeId = 0);
        Task<string> GetNextInvoiceNoAsync();
        Task<int> CreateAsync(Invoice invoice);
        Task<bool> UpdateAsync(Invoice invoice);
        Task<bool> MarkAsPaidAsync(int invoiceId);
        Task<bool> DeleteAsync(int invoiceId);
    }
}
