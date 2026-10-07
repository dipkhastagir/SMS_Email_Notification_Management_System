using SMSNotificationSystem.Models;

namespace SMSNotificationSystem.Repositories.Interfaces
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(int id);
        Task<int> CreateAsync(Product product);
        Task<bool> UpdateAsync(Product product);

        /// <summary>Adds (positive) or removes (negative) stock. Never goes below zero.</summary>
        Task<bool> AdjustStockAsync(int productId, int delta);
        Task<bool> DeleteAsync(int productId);
    }
}
