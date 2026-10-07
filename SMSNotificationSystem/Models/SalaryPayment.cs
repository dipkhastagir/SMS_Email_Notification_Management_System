namespace SMSNotificationSystem.Models
{
    public class SalaryPayment
    {
        public int PaymentId { get; set; }
        public int EmployeeId { get; set; }
        public string SalaryMonth { get; set; } = string.Empty;   // yyyy-MM
        public decimal Amount { get; set; }
        public DateTime PaidAt { get; set; } = DateTime.Now;
        public int? PaidBy { get; set; }
    }
}
