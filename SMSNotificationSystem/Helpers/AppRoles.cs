namespace SMSNotificationSystem.Helpers
{
    /// <summary>Role names used by [Authorize(Roles = ...)] and stored in Users.Role.</summary>
    public static class AppRoles
    {
        public const string Administrator = "Administrator";
        public const string Accountant = "Accountant";
        public const string HR = "HR";
        public const string Manager = "Manager";

        // Comma separated combinations for [Authorize(Roles = ...)]
        public const string AdminOrManager = Administrator + "," + Manager;
        public const string AdminOrAccountant = Administrator + "," + Accountant;
        public const string AdminOrHR = Administrator + "," + HR;
        public const string Payroll = Administrator + "," + HR + "," + Accountant;
        public const string Inventory = Administrator + "," + Manager;

        public static readonly string[] All = { Administrator, Accountant, HR, Manager };
    }
}
