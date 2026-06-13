using Bondstone.Messaging;
using Bondstone.Persistence;

namespace Bondstone.Transport.RabbitMq.Outbox;

internal sealed class RabbitMqDurableOutboxTransportRoute(
    IRabbitMqMessagePublisher publisher,
    RabbitMqCommandRoutingTopology commandTopology,
    RabbitMqEventRoutingTopology eventTopology)
    : IDurableOutboxTransportRoute
{
    private readonly RabbitMqDurableOutboxTransport _transport =
        new(
            publisher ?? throw new ArgumentNullException(nameof(publisher)),
            new RabbitMqCommandRouteResolver(commandTopology),
            new RabbitMqEventRouteResolver(eventTopology));
    private readonly RabbitMqCommandRoutingTopology _commandTopology =
        commandTopology ?? throw new ArgumentNullException(nameof(commandTopology));
    private readonly RabbitMqEventRoutingTopology _eventTopology =
        eventTopology ?? throw new ArgumentNullException(nameof(eventTopology));

    public string TransportName => "RabbitMq";

    public bool CanSend(
        DurableOutboxRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        DurableMessageEnvelope envelope = record.Envelope;
        if (envelope.MessageKind == MessageKind.Command)
        {
            return _commandTopology.DescribeRoute(envelope.TargetModule!).HasRoute;
        }

        if (envelope.MessageKind == MessageKind.Event)
        {
            return _eventTopology.DescribeRoute(envelope.MessageTypeName).HasRoute;
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
