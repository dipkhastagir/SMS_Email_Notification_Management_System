using System.Globalization;

namespace SMSNotificationSystem.Helpers
{
    /// <summary>
    /// Evaluates a trigger rule such as  StockQty &lt;= ReorderLevel  or  DaysUntilDue &lt;= 3
    /// against the field values of one business record.
    /// The right-hand side may be a literal or the name of another field of the same record.
    /// </summary>
    public static class ConditionEvaluator
    {
        public static bool Evaluate(IReadOnlyDictionary<string, object?> record, string field, string op, string value)
        {
            if (!record.TryGetValue(field, out var leftRaw) || leftRaw == null)
                return false;

            object? rightRaw = record.TryGetValue(value.Trim(), out var fieldValue) ? fieldValue : value.Trim();
            if (rightRaw == null) return false;

            if (TryNumber(leftRaw, out var left) && TryNumber(rightRaw, out var right))
            {
                return op switch
                {
                    "<" => left < right,
                    "<=" => left <= right,
                    ">" => left > right,
                    ">=" => left >= right,
                    "=" => left == right,
                    "!=" => left != right,
                    _ => false
                };
            }

            var l = Convert.ToString(leftRaw, CultureInfo.InvariantCulture) ?? string.Empty;
            var r = Convert.ToString(rightRaw, CultureInfo.InvariantCulture) ?? string.Empty;
            var cmp = string.Compare(l, r, StringComparison.OrdinalIgnoreCase);

            return op switch
            {
                "=" => cmp == 0,
                "!=" => cmp != 0,
                "<" => cmp < 0,
                "<=" => cmp <= 0,
                ">" => cmp > 0,
                ">=" => cmp >= 0,
                _ => false
            };
        }

        private static bool TryNumber(object raw, out decimal number)
        {
            switch (raw)
            {
                case decimal d: number = d; return true;
                case int i: number = i; return true;
                case long l: number = l; return true;
                case double db: number = (decimal)db; return true;
                default:
                    return decimal.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture),
                        NumberStyles.Number, CultureInfo.InvariantCulture, out number);
            }
        }
    }
}
