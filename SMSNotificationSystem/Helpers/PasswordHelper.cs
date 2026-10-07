namespace SMSNotificationSystem.Helpers
{
    /// <summary>One-way, per-user salted password hashing with BCrypt (adaptive work factor).</summary>
    public static class PasswordHelper
    {
        private const int WorkFactor = 11;

        public static string HashPassword(string password) =>
            BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword)) return false;
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
