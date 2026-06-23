using Bondstone.Messaging;

namespace Bondstone.Transport.ServiceBus;

public sealed class ServiceBusEnvelopeDispatcherOptions
{
    public Func<DurableMessageEnvelope, string>? ResolveEntityName { get; set; }

    public string ContentType { get; set; } = "application/vnd.bondstone.envelope+json";

    internal string GetEntityName(
        DurableMessageEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (ResolveEntityName is null)
        {
            throw new InvalidOperationException(
                $"{nameof(ServiceBusEnvelopeDispatcherOptions)} requires {nameof(ResolveEntityName)}.");
        }

        string entityName = ResolveEntityName(envelope);
        if (string.IsNullOrWhiteSpace(entityName))
        {
            throw new InvalidOperationException(
                $"{nameof(ResolveEntityName)} returned an empty Service Bus entity name for message '{envelope.MessageId}'.");
        }

        return entityName;
    }
}
