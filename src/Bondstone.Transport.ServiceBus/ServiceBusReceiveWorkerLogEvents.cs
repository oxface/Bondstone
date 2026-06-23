using Microsoft.Extensions.Logging;

namespace Bondstone.Transport.ServiceBus;

internal static class ServiceBusReceiveWorkerLogEvents
{
    public static readonly EventId ReceiveFailed = new(
        3001,
        nameof(ReceiveFailed));
}
