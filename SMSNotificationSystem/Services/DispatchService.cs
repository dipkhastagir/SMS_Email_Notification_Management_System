using Microsoft.Extensions.Options;
using SMSNotificationSystem.DTOs;
using SMSNotificationSystem.Helpers;
using SMSNotificationSystem.Models;
using SMSNotificationSystem.Repositories.Interfaces;
using SMSNotificationSystem.Services.Interfaces;

namespace SMSNotificationSystem.Services
{
    /// <summary>Message queue -> SMS / Email gateway -> delivery log.</summary>
    public class DispatchService : IDispatchService
    {
        private readonly IQueueRepository _queueRepository;
        private readonly IGatewaySettingRepository _gatewayRepository;
        private readonly INotificationGateway _gateway;
        private readonly NotificationOptions _options;
        private readonly ILogger<DispatchService> _logger;

        public DispatchService(
            IQueueRepository queueRepository,
            IGatewaySettingRepository gatewayRepository,
            INotificationGateway gateway,
            IOptions<NotificationOptions> options,
            ILogger<DispatchService> logger)
        {
            _queueRepository = queueRepository;
            _gatewayRepository = gatewayRepository;
            _gateway = gateway;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<DispatchSummary> ProcessQueueAsync(CancellationToken cancellationToken = default)
        {
            var summary = new DispatchSummary();
            var batch = (await _queueRepository.ClaimPendingAsync(Math.Max(1, _options.DispatchBatchSize))).ToList();
            summary.Claimed = batch.Count;
            if (batch.Count == 0) return summary;

            // one gateway lookup per channel per run
            var gateways = new Dictionary<string, GatewaySetting?>();

            foreach (var message in batch)
            {
                if (!gateways.TryGetValue(message.Channel, out var gatewaySetting))
                {
                    gatewaySetting = await _gatewayRepository.GetPrimaryActiveAsync(message.Channel);
                    gateways[message.Channel] = gatewaySetting;
                }

                GatewayResult result;
                try
                {
                    result = await _gateway.SendAsync(message, gatewaySetting, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Gateway threw while sending message {MessageId}", message.MessageId);
                    result = GatewayResult.Failure(gatewaySetting?.Provider ?? "None", "EXCEPTION", ex.Message);
                }

                var newStatus = await _queueRepository.RecordResultAsync(message.MessageId, result, Math.Max(1, _options.MaxRetries));
                switch (newStatus)
                {
                    case "Sent": summary.Sent++; break;
                    case "Retrying": summary.Retrying++; break;
                    default: summary.Failed++; break;
                }
            }

            _logger.LogInformation("Dispatch run: {Summary}", summary);
            return summary;
        }
    }
}
