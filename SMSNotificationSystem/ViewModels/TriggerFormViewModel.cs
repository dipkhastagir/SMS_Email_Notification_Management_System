using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SMSNotificationSystem.ViewModels
{
    public class TriggerFormViewModel
    {
        public int TriggerId { get; set; }

        [Required(ErrorMessage = "Give the trigger a name")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Choose the event to watch")]
        [Display(Name = "Event")]
        public string EventType { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Choose a template")]
        [Display(Name = "Template")]
        public int TemplateId { get; set; }

        [Required(ErrorMessage = "Choose a field")]
        [Display(Name = "Field")]
        public string ConditionField { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Operator")]
        public string ConditionOperator { get; set; } = "<=";

        [Required(ErrorMessage = "Enter a value")]
        [StringLength(50)]
        [Display(Name = "Value")]
        public string ConditionValue { get; set; } = string.Empty;

        [StringLength(120)]
        [Display(Name = "Recipient name")]
        public string? RecipientName { get; set; }

        [StringLength(150)]
        [Display(Name = "Recipient phone or email")]
        public string? RecipientContact { get; set; }

        [Range(1, 720, ErrorMessage = "Between 1 and 720 hours")]
        [Display(Name = "Repeat no more than every (hours)")]
        public int CooldownHours { get; set; } = 24;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [ValidateNever]
        public IEnumerable<SelectListItem> Templates { get; set; } = Enumerable.Empty<SelectListItem>();

        /// <summary>TemplateId -> channel, used by the form to hint which contact type is needed.</summary>
        [ValidateNever]
        public Dictionary<int, string> TemplateChannels { get; set; } = new();

        public bool IsEdit => TriggerId > 0;
    }
}
