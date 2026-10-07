namespace SMSNotificationSystem.Helpers
{
    /// <summary>Small helpers used by Razor views to keep markup consistent.</summary>
    public static class UiHelper
    {
        public static string StatusClass(string? status) => status switch
        {
            "Sent" or "Active" or "Paid" or "Present" => "pill-ok",
            "Failed" or "Absent" or "Inactive" or "Overdue" => "pill-bad",
            "Retrying" or "Late" or "Due soon" => "pill-warn",
            "Pending" or "Processing" or "Leave" or "Unpaid" => "pill-info",
            "Cancelled" => "pill-muted",
            _ => "pill-muted"
        };

        public static string ChannelIcon(string? channel) =>
            channel == "Email" ? "bi-envelope" : "bi-phone";

        public static string Taka(decimal amount) => "Tk " + amount.ToString("N2");

        public static string MaskSecret(string? secret)
        {
            if (string.IsNullOrEmpty(secret)) return "—";
            return secret.Length <= 4 ? "••••" : new string('•', 8) + secret[^4..];
        }

        public static string TimeAgo(DateTime? value)
        {
            if (value == null) return "never";
            var span = DateTime.Now - value.Value;
            if (span.TotalSeconds < 60) return "just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} h ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays} d ago";
            return value.Value.ToString("dd MMM yyyy");
        }
    }
}
