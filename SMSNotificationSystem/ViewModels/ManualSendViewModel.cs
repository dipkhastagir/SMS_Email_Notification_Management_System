using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SMSNotificationSystem.Models;

namespace SMSNotificationSystem.ViewModels
{
    public class ManualSendViewModel
    {
        [Display(Name = "Start from a template")]
        public int? TemplateId { get; set; }

        [Required(ErrorMessage = "Enter the recipient's name")]
        [StringLength(120)]
        [Display(Name = "Recipient name")]
        public string RecipientName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a phone number or email")]
        [StringLength(150)]
        [Display(Name = "Phone or email")]
        public string RecipientContact { get; set; } = string.Empty;

        [Required]
        public string Channel { get; set; } = "SMS";

        [StringLength(200)]
        [Display(Name = "Email subject")]
        public string? Subject { get; set; }

        [Required(ErrorMessage = "Write the message")]
        [StringLength(1000)]
        [Display(Name = "Message")]
        public string MessageBody { get; set; } = string.Empty;

        [ValidateNever]
        public IEnumerable<MessageTemplate> Templates { get; set; } = Enumerable.Empty<MessageTemplate>();
    }
}
