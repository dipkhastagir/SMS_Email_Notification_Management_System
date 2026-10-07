using SMSNotificationSystem.Models;

namespace SMSNotificationSystem.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(int id);
        Task<IEnumerable<User>> GetAllAsync();
        Task<bool> EmailExistsAsync(string email, int excludeUserId = 0);
        Task<int> CreateAsync(User user);
        Task<bool> UpdateAsync(User user);
        Task<bool> UpdatePasswordAsync(int userId, string passwordHash);
        Task<bool> SetActiveAsync(int userId, bool isActive);
        Task UpdateLastLoginAsync(int userId);
    }
}
