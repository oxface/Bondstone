using Azure.Messaging.ServiceBus;

namespace Bondstone.Transport.ServiceBus.Inbox;

internal sealed class ServiceBusReceivedMessageHandler(
    IServiceBusReceivedMessageDispatcher dispatcher)
    : IServiceBusReceivedMessageHandler
{
    private readonly IServiceBusReceivedMessageDispatcher _dispatcher =
        dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

    public async ValueTask HandleAsync(
        ServiceBusReceiveSource source,
        ServiceBusReceivedMessage message,
        Func<ServiceBusReceivedMessage, CancellationToken, ValueTask> completeAsync,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(completeAsync);

        await _dispatcher.DispatchAsync(
            source,
            ServiceBusReceivedMessageMapper.FromReceivedMessage(message),
            ct);
        await completeAsync(message, ct);
    }
}
