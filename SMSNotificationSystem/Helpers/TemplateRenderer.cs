using System.Text.RegularExpressions;

namespace SMSNotificationSystem.Helpers
{
    /// <summary>Replaces {Placeholder} tokens in a template body with real values.</summary>
    public static partial class TemplateRenderer
    {
        [GeneratedRegex(@"\{(\w+)\}")]
        private static partial Regex PlaceholderRegex();

        public static string Render(string? template, IReadOnlyDictionary<string, string> values)
        {
            if (string.IsNullOrEmpty(template)) return string.Empty;

            return PlaceholderRegex().Replace(template, match =>
            {
                var key = match.Groups[1].Value;
                return values.TryGetValue(key, out var value) ? value : match.Value;
            });
        }

        public static IEnumerable<string> ExtractPlaceholders(string? template)
        {
            if (string.IsNullOrEmpty(template)) return Enumerable.Empty<string>();
            return PlaceholderRegex().Matches(template).Select(m => m.Value).Distinct();
        }

        /// <summary>Values that are available in every template.</summary>
        public static Dictionary<string, string> GlobalValues(string companyName, string recipientName) => new()
        {
            ["CompanyName"] = companyName,
            ["Today"] = DateTime.Today.ToString("dd MMM yyyy"),
            ["RecipientName"] = recipientName
        };
    }
}
