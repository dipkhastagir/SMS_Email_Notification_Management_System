using System.Security.Claims;

namespace SMSNotificationSystem.Helpers
{
    public static class ClaimsExtensions
    {
        public static int? GetUserId(this ClaimsPrincipal user) =>
            int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        public static string GetRole(this ClaimsPrincipal user) =>
            user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        public static string GetInitials(this ClaimsPrincipal user)
        {
            var name = user.Identity?.Name ?? "?";
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch
            {
                0 => "?",
                1 => parts[0][..1].ToUpperInvariant(),
                _ => (parts[0][..1] + parts[^1][..1]).ToUpperInvariant()
            };
        }
    }
}
