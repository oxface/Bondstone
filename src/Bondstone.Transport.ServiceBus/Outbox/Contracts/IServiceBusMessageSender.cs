namespace Bondstone.Transport.ServiceBus.Outbox;

public interface IServiceBusMessageSender
{
    ValueTask SendAsync(
        string entityName,
        ServiceBusTransportMessage message,
        CancellationToken ct = default);
}
