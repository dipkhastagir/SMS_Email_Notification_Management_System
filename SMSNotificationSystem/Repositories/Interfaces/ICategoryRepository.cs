using SMSNotificationSystem.Models;

namespace SMSNotificationSystem.Repositories.Interfaces
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<TemplateCategory>> GetAllAsync();
        Task<bool> NameExistsAsync(string name);
        Task<int> CreateAsync(TemplateCategory category);
        Task<bool> DeleteAsync(int id);
    }
}
