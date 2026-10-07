namespace SMSNotificationSystem.DTOs
{
    /// <summary>One row from dbo.vw_AttendanceDetails.</summary>
    public class AttendanceWithEmployeeDto
    {
        public int AttendanceId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateTime AttendanceDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }
}
