using SMSNotificationSystem.DTOs;

namespace SMSNotificationSystem.Repositories.Interfaces
{
    public interface IAttendanceRepository
    {
        Task<IEnumerable<AttendanceWithEmployeeDto>> GetByDateAsync(DateTime date);
        Task<IEnumerable<AttendanceWithEmployeeDto>> GetRecentAsync(int days);
        Task UpsertAsync(int employeeId, DateTime date, string status, string? remarks);
    }
}
