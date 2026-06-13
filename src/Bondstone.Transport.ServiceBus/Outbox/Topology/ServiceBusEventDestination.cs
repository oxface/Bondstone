using Bondstone.Utility;

namespace Bondstone.Transport.ServiceBus.Outbox;

public sealed class ServiceBusEventDestination
{
    public ServiceBusEventDestination(
        ServiceBusEventDestinationKind kind,
        string entityName)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Service Bus event destination kind is not supported.");
        }

        Kind = kind;
        EntityName = entityName.NormalizeRequired(
            nameof(entityName),
            "Service Bus event destination entity name");
    }

    public ServiceBusEventDestinationKind Kind { get; }

    public string EntityName { get; }
}
