namespace SMSNotificationSystem.Models
{
    public class Attendance
    {
        public int AttendanceId { get; set; }
        public int EmployeeId { get; set; }
        public DateTime AttendanceDate { get; set; } = DateTime.Today;
        public string Status { get; set; } = "Present";   // Present | Absent | Late | Leave
        public string? Remarks { get; set; }

        public static readonly string[] Statuses = { "Present", "Absent", "Late", "Leave" };
    }
}
