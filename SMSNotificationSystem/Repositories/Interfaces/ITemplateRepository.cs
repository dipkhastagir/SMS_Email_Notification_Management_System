using SMSNotificationSystem.DTOs;
using SMSNotificationSystem.Models;

namespace SMSNotificationSystem.Repositories.Interfaces
{
    public interface ITemplateRepository
    {
        Task<IEnumerable<TemplateWithCategoryDto>> GetAllAsync();
        Task<IEnumerable<MessageTemplate>> GetActiveAsync();
        Task<MessageTemplate?> GetByIdAsync(int id);
        Task<int> CreateAsync(MessageTemplate template);
        Task<bool> UpdateAsync(MessageTemplate template);
        Task<bool> SetStatusAsync(int id, string status);
        Task<bool> IsUsedByTriggerAsync(int id);
        Task<bool> DeleteAsync(int id);
    }
}
