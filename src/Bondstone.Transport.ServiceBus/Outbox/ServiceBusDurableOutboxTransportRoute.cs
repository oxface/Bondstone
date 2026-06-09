using Bondstone.Messaging;
using Bondstone.Persistence;

namespace Bondstone.Transport.ServiceBus.Outbox;

internal sealed class ServiceBusDurableOutboxTransportRoute(
    IServiceBusMessageSender messageSender,
    ServiceBusCommandDestinationTopology commandTopology,
    ServiceBusEventDestinationTopology eventDestinationTopology)
    : IDurableOutboxTransportRoute
{
    private readonly ServiceBusDurableOutboxTransport _transport =
        new(
            messageSender ?? throw new ArgumentNullException(nameof(messageSender)),
            new ServiceBusModuleQueueResolver(commandTopology),
            new ServiceBusEventDestinationResolver(eventDestinationTopology));
    private readonly ServiceBusCommandDestinationTopology _commandTopology =
        commandTopology ?? throw new ArgumentNullException(nameof(commandTopology));
    private readonly ServiceBusEventDestinationTopology _eventDestinationTopology =
        eventDestinationTopology ?? throw new ArgumentNullException(nameof(eventDestinationTopology));

    public string TransportName => "ServiceBus";

    public bool CanSend(
        DurableOutboxRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        DurableMessageEnvelope envelope = record.Envelope;
        if (envelope.MessageKind == MessageKind.Command)
        {
            return _commandTopology.DescribeDestination(envelope.TargetModule!).HasDestination;
        }

        if (envelope.MessageKind == MessageKind.Event)
        {
            return _eventDestinationTopology.DescribeDestination(envelope.MessageTypeName).HasDestination;
        }

        return false;
    }

    public ValueTask SendAsync(
        DurableOutboxRecord record,
        CancellationToken ct = default)
    {
        return _transport.SendAsync(record, ct);
    }
}
