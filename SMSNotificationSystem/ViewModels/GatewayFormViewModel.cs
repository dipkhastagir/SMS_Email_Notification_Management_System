using System.ComponentModel.DataAnnotations;

namespace SMSNotificationSystem.ViewModels
{
    public class GatewayFormViewModel
    {
        public int GatewayId { get; set; }

        [Required, StringLength(80)]
        [Display(Name = "Provider name")]
        public string Provider { get; set; } = string.Empty;

        [Required]
        public string Channel { get; set; } = "SMS";

        /// <summary>Required when creating. When editing, leave blank to keep the saved key.</summary>
        [StringLength(200)]
        [Display(Name = "API key")]
        public string? ApiKey { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Sender ID or from address")]
        public string SenderId { get; set; } = string.Empty;

        [Display(Name = "Primary gateway for this channel")]
        public bool IsPrimary { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public string? MaskedApiKey { get; set; }

        public bool IsEdit => GatewayId > 0;
    }
}
