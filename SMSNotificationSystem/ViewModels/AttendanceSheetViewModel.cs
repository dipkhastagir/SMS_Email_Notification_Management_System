using SMSNotificationSystem.DTOs;

namespace SMSNotificationSystem.ViewModels
{
    public class AttendanceSheetViewModel
    {
        public DateTime Date { get; set; } = DateTime.Today;
        public List<AttendanceSheetRow> Rows { get; set; } = new();
        public List<AttendanceWithEmployeeDto> RecentExceptions { get; set; } = new();
    }

    public class AttendanceSheetRow
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Status { get; set; } = "Present";
        public string? Remarks { get; set; }
        public bool IsRecorded { get; set; }
    }
}
