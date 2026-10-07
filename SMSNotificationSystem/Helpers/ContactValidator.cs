using System.Net.Mail;
using System.Text.RegularExpressions;

namespace SMSNotificationSystem.Helpers
{
    public static partial class ContactValidator
    {
        // +8801XXXXXXXXX, 01XXXXXXXXX or any international number with 10-15 digits
        [GeneratedRegex(@"^\+?[0-9]{10,15}$")]
        private static partial Regex PhoneRegex();

        public static bool IsValidPhone(string? phone) =>
            !string.IsNullOrWhiteSpace(phone) && PhoneRegex().IsMatch(phone.Replace(" ", "").Replace("-", ""));

        public static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                var address = new MailAddress(email.Trim());
                return address.Address == email.Trim() && email.Contains('.');
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static bool IsValidFor(string channel, string? contact) =>
            channel == "Email" ? IsValidEmail(contact) : IsValidPhone(contact);

        public static string Normalize(string channel, string contact) =>
            channel == "Email" ? contact.Trim().ToLowerInvariant() : contact.Replace(" ", "").Replace("-", "").Trim();
    }
}
