using SMSNotificationSystem.DTOs;
using SMSNotificationSystem.Models;

namespace SMSNotificationSystem.Repositories.Interfaces
{
    public interface ITriggerRepository
    {
        Task<IEnumerable<TriggerWithTemplateDto>> GetAllAsync();
        Task<IEnumerable<EventTrigger>> GetActiveAsync();
        Task<EventTrigger?> GetByIdAsync(int id);
        Task<int> CreateAsync(EventTrigger trigger);
        Task<bool> UpdateAsync(EventTrigger trigger);
        Task<bool> SetActiveAsync(int id, bool isActive);
        Task UpdateLastRunAsync(int id);
        Task<bool> DeleteAsync(int id);
    }
}
