using System.ComponentModel.DataAnnotations;

namespace SMSNotificationSystem.Models
{
    public class Invoice
    {
        public int InvoiceId { get; set; }

        [Required, StringLength(30), Display(Name = "Invoice no.")]
        public string InvoiceNo { get; set; } = string.Empty;

        [Required, StringLength(120), Display(Name = "Customer name")]
        public string CustomerName { get; set; } = string.Empty;

        [Required, StringLength(20), Display(Name = "Customer phone")]
        [RegularExpression(@"^\+?[0-9]{10,15}$", ErrorMessage = "Use a number like +8801711000101")]
        public string CustomerPhone { get; set; } = string.Empty;

        [EmailAddress, StringLength(150), Display(Name = "Customer email")]
        public string? CustomerEmail { get; set; }

        [Range(0.01, 999999999, ErrorMessage = "Amount must be greater than zero")]
        public decimal Amount { get; set; }

        [DataType(DataType.Date), Display(Name = "Due date")]
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(7);

        public bool IsPaid { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int DaysUntilDue => (DueDate.Date - DateTime.Today).Days;

        public string PaymentState => IsPaid ? "Paid" : DaysUntilDue < 0 ? "Overdue" : DaysUntilDue <= 3 ? "Due soon" : "Unpaid";
    }
}
