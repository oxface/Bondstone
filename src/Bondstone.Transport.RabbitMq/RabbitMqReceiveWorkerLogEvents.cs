using Microsoft.Extensions.Logging;

namespace Bondstone.Transport.RabbitMq;

internal static class RabbitMqReceiveWorkerLogEvents
{
    public static readonly EventId ReceiveFailed = new(
        2001,
        nameof(ReceiveFailed));
}
