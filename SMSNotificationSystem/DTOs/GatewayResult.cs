namespace SMSNotificationSystem.DTOs
{
    /// <summary>What a gateway returns for one send attempt.</summary>
    public class GatewayResult
    {
        public bool IsSuccess { get; init; }
        public string Provider { get; init; } = string.Empty;
        public string Response { get; init; } = string.Empty;
        public string? FailureReason { get; init; }

        /// <summary>True when retrying cannot help (bad number, no gateway configured...).</summary>
        public bool IsPermanentFailure { get; init; }

        public static GatewayResult Success(string provider, string response) =>
            new() { IsSuccess = true, Provider = provider, Response = response };

        public static GatewayResult Failure(string provider, string response, string reason, bool permanent = false) =>
            new() { IsSuccess = false, Provider = provider, Response = response, FailureReason = reason, IsPermanentFailure = permanent };
    }
}
