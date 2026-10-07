namespace SMSNotificationSystem.DTOs
{
    /// <summary>One row from dbo.vw_SalaryPayments.</summary>
    public class SalaryPaymentDto
    {
        public int PaymentId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string SalaryMonth { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime PaidAt { get; set; }
        public string? PaidByName { get; set; }
    }
}
