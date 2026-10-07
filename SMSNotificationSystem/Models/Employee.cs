using System.ComponentModel.DataAnnotations;

namespace SMSNotificationSystem.Models
{
    public class Employee
    {
        public int EmployeeId { get; set; }

        [Required, StringLength(120), Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required, StringLength(20)]
        [RegularExpression(@"^\+?[0-9]{10,15}$", ErrorMessage = "Use a number like +8801712000201")]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress, StringLength(150)]
        public string? Email { get; set; }

        [Required, StringLength(80)]
        public string Department { get; set; } = string.Empty;

        [StringLength(80)]
        public string? Designation { get; set; }

        [Range(0, 999999999)]
        public decimal Salary { get; set; }

        public bool IsActive { get; set; } = true;

        [DataType(DataType.Date), Display(Name = "Joined on")]
        public DateTime JoinedAt { get; set; } = DateTime.Today;
    }
}
