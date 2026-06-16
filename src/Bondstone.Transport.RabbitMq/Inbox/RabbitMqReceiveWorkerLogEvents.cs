using Microsoft.Extensions.Logging;

namespace Bondstone.Transport.RabbitMq.Inbox;

internal static class RabbitMqReceiveWorkerLogEvents
{
    public static readonly EventId DeliveryHandlingFailed = new(
        3001,
        nameof(DeliveryHandlingFailed));
}
