using System.ComponentModel.DataAnnotations;

namespace SMSNotificationSystem.ViewModels
{
    public class UserFormViewModel
    {
        public int UserId { get; set; }

        [Required, StringLength(120), Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Choose a role")]
        public string Role { get; set; } = string.Empty;

        /// <summary>Required when creating; optional when editing (blank keeps the current password).</summary>
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Use at least 8 characters")]
        public string? Password { get; set; }

        public bool IsEdit => UserId > 0;
    }
}
