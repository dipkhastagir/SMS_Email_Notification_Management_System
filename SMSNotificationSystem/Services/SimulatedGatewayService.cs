using Microsoft.Extensions.Options;
using SMSNotificationSystem.DTOs;
using SMSNotificationSystem.Helpers;
using SMSNotificationSystem.Models;
using SMSNotificationSystem.Services.Interfaces;

namespace SMSNotificationSystem.Services
{
    /// <summary>
    /// Development gateway that behaves like a real provider without sending anything:
    /// it validates the recipient, waits a little, and succeeds or fails at a configurable rate,
    /// so the queue, retry and delivery-log logic can be tested end to end.
    /// </summary>
    public class SimulatedGatewayService : INotificationGateway
    {
        private static readonly (string Code, string Reason)[] TransientErrors =
        {
            ("ERROR 504", "Gateway timeout"),
            ("ERROR 503", "Network unreachable"),
            ("ERROR 429", "Rate limit exceeded")
        };

        private readonly NotificationOptions _options;
        private readonly ILogger<SimulatedGatewayService> _logger;

        public SimulatedGatewayService(IOptions<NotificationOptions> options, ILogger<SimulatedGatewayService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<GatewayResult> SendAsync(MessageQueue message, GatewaySetting? gateway, CancellationToken cancellationToken = default)
        {
            if (gateway == null)
            {
                return GatewayResult.Failure("None", "NO_GATEWAY",
                    $"No active {message.Channel} gateway is configured", permanent: true);
            }

            if (!ContactValidator.IsValidFor(message.Channel, message.RecipientContact))
            {
                return GatewayResult.Failure(gateway.Provider, "ERROR 400",
                    message.Channel == "Email" ? "Invalid email address" : "Invalid phone number", permanent: true);
            }

            if (message.Channel == "SMS" && message.MessageBody.Length > 918)
            {
                return GatewayResult.Failure(gateway.Provider, "ERROR 413",
                    "Message longer than 6 SMS segments", permanent: true);
            }

            // simulate network latency
            await Task.Delay(Random.Shared.Next(150, 450), cancellationToken);

            var successRate = Math.Clamp(_options.SimulatedSuccessRate, 0, 100);
            if (Random.Shared.Next(1, 101) <= successRate)
            {
                var reference = $"SIM{Random.Shared.Next(100000, 999999)}";
                var segments = message.Channel == "SMS" ? Math.Max(1, (int)Math.Ceiling(message.MessageBody.Length / 153.0)) : 1;
                _logger.LogInformation("[{Provider}] {Channel} to {Contact} accepted, ref {Ref}",
                    gateway.Provider, message.Channel, message.RecipientContact, reference);

                return GatewayResult.Success(gateway.Provider,
                    $"ACCEPTED ref={reference} from={gateway.SenderId} segments={segments}");
            }

            var error = TransientErrors[Random.Shared.Next(TransientErrors.Length)];
            _logger.LogWarning("[{Provider}] {Channel} to {Contact} failed: {Reason}",
                gateway.Provider, message.Channel, message.RecipientContact, error.Reason);
            return GatewayResult.Failure(gateway.Provider, error.Code, error.Reason);
        }
    }
}
