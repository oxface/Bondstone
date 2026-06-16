using Bondstone.Messaging;

namespace Bondstone.Transport.RabbitMq;

public sealed class RabbitMqEnvelopeDispatcherOptions
{
    public Func<DurableMessageEnvelope, RabbitMqEnvelopeDestination>? ResolveDestination { get; set; }

    public bool Mandatory { get; set; } = true;

    internal RabbitMqEnvelopeDestination GetDestination(
        DurableMessageEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (ResolveDestination is null)
        {
            throw new InvalidOperationException(
                $"{nameof(RabbitMqEnvelopeDispatcherOptions)} requires {nameof(ResolveDestination)}.");
        }

        RabbitMqEnvelopeDestination destination = ResolveDestination(envelope);
        if (string.IsNullOrWhiteSpace(destination.Exchange))
        {
            throw new InvalidOperationException(
                $"{nameof(ResolveDestination)} returned an empty RabbitMQ exchange for message '{envelope.MessageId}'.");
        }

        if (string.IsNullOrWhiteSpace(destination.RoutingKey))
        {
            throw new InvalidOperationException(
                $"{nameof(ResolveDestination)} returned an empty RabbitMQ routing key for message '{envelope.MessageId}'.");
        }

        return destination;
    }
}
