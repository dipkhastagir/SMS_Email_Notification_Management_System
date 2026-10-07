namespace SMSNotificationSystem.Helpers
{
    /// <summary>Describes one kind of business event that a trigger can watch.</summary>
    public sealed record EventTypeInfo(
        string Key,
        string DisplayName,
        string Description,
        string Icon,
        string[] Fields,
        string[] Placeholders,
        string DefaultField,
        string DefaultOperator,
        string DefaultValue,
        bool OneTimePerRecord);

    public static class EventTypes
    {
        public const string InvoiceDue = "InvoiceDue";
        public const string StockLow = "StockLow";
        public const string AttendanceAbsent = "AttendanceAbsent";
        public const string SalaryDisbursed = "SalaryDisbursed";

        public static readonly string[] Operators = { "<", "<=", ">", ">=", "=", "!=" };

        /// <summary>Placeholders that work in every template.</summary>
        public static readonly string[] GlobalPlaceholders = { "{CompanyName}", "{Today}", "{RecipientName}" };

        public static readonly IReadOnlyDictionary<string, EventTypeInfo> All = new Dictionary<string, EventTypeInfo>
        {
            [InvoiceDue] = new(InvoiceDue, "Invoice payment due",
                "Unpaid invoices that are close to (or past) their due date.",
                "bi-receipt",
                new[] { "DaysUntilDue", "Amount" },
                new[] { "{CustomerName}", "{InvoiceNo}", "{DueAmount}", "{DueDate}", "{DaysLeft}" },
                "DaysUntilDue", "<=", "3", false),

            [StockLow] = new(StockLow, "Low stock",
                "Products whose stock quantity falls to the reorder level.",
                "bi-box-seam",
                new[] { "StockQty", "ReorderLevel" },
                new[] { "{ProductName}", "{Sku}", "{StockQty}", "{ReorderLevel}" },
                "StockQty", "<=", "ReorderLevel", false),

            [AttendanceAbsent] = new(AttendanceAbsent, "Attendance alert",
                "Employees marked absent, late or on leave today.",
                "bi-person-x",
                new[] { "Status" },
                new[] { "{EmployeeName}", "{Department}", "{Date}", "{Status}" },
                "Status", "=", "Absent", true),

            [SalaryDisbursed] = new(SalaryDisbursed, "Salary disbursed",
                "Salary payments recorded in the last three days.",
                "bi-cash-coin",
                new[] { "Amount" },
                new[] { "{EmployeeName}", "{Amount}", "{Month}" },
                "Amount", ">", "0", true)
        };

        public static string DisplayName(string? key) =>
            key != null && All.TryGetValue(key, out var info) ? info.DisplayName : "Manual message";

        public static string Icon(string? key) =>
            key != null && All.TryGetValue(key, out var info) ? info.Icon : "bi-send";
    }
}
