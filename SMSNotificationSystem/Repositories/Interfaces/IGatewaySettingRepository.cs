using SMSNotificationSystem.Models;

namespace SMSNotificationSystem.Repositories.Interfaces
{
    public interface IGatewaySettingRepository
    {
        Task<IEnumerable<GatewaySetting>> GetAllAsync();
        Task<GatewaySetting?> GetByIdAsync(int id);
        Task<GatewaySetting?> GetPrimaryActiveAsync(string channel);
        Task<int> CreateAsync(GatewaySetting setting);
        Task<bool> UpdateAsync(GatewaySetting setting);
        Task<bool> DeleteAsync(int id);
    }
}
