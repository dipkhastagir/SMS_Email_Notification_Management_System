using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SMSNotificationSystem.ViewModels
{
    public class TemplateFormViewModel
    {
        public int TemplateId { get; set; }

        [Required(ErrorMessage = "Give the template a name")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Choose a category")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required]
        public string Channel { get; set; } = "SMS";

        [StringLength(200)]
        [Display(Name = "Email subject")]
        public string? Subject { get; set; }

        [Required(ErrorMessage = "Write the message")]
        [StringLength(1000)]
        [Display(Name = "Message")]
        public string Body { get; set; } = string.Empty;

        public string Status { get; set; } = "Active";

        [ValidateNever]
        public IEnumerable<SelectListItem> Categories { get; set; } = Enumerable.Empty<SelectListItem>();

        public bool IsEdit => TemplateId > 0;
    }
}
