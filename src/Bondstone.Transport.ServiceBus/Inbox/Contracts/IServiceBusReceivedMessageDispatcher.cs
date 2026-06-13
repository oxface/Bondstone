using Bondstone.Transport.ServiceBus.Outbox;

namespace Bondstone.Transport.ServiceBus.Inbox;

public interface IServiceBusReceivedMessageDispatcher
{
    ValueTask DispatchAsync(
        ServiceBusReceiveSource source,
        ServiceBusTransportMessage message,
        CancellationToken ct = default);
}
