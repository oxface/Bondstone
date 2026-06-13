using Azure.Messaging.ServiceBus;

namespace Bondstone.Transport.ServiceBus.Inbox;

public interface IServiceBusReceivedMessageHandler
{
    ValueTask HandleAsync(
        ServiceBusReceiveSource source,
        ServiceBusReceivedMessage message,
        Func<ServiceBusReceivedMessage, CancellationToken, ValueTask> completeAsync,
        CancellationToken ct = default);
}
