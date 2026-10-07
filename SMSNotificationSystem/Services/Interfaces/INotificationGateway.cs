using SMSNotificationSystem.DTOs;
using SMSNotificationSystem.Models;

namespace SMSNotificationSystem.Services.Interfaces
{
    /// <summary>
    /// Abstraction over an SMS or Email provider. The project ships with a simulated
    /// implementation; a real Alpha SMS, Twilio or SMTP class only needs to implement this.
    /// </summary>
    public interface INotificationGateway
    {
        Task<GatewayResult> SendAsync(MessageQueue message, GatewaySetting? gateway, CancellationToken cancellationToken = default);
    }
}
