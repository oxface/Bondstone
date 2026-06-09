using Bondstone.Utility;

namespace Bondstone.Transport.ServiceBus.Outbox;

internal sealed class ServiceBusEventDestinationTopology
{
    private readonly IReadOnlyDictionary<string, ServiceBusEventDestination> _destinationsByMessageTypeName;
    private readonly ServiceBusEventDestinationConvention? _destinationConvention;

    public ServiceBusEventDestinationTopology(
        IReadOnlyDictionary<string, ServiceBusEventDestination> destinationsByMessageTypeName,
        ServiceBusEventDestinationConvention? destinationConvention = null)
    {
        ArgumentNullException.ThrowIfNull(destinationsByMessageTypeName);

        _destinationsByMessageTypeName = destinationsByMessageTypeName
            .Select(static entry => new KeyValuePair<string, ServiceBusEventDestination>(
                entry.Key.NormalizeRequired("messageTypeName", "Message type name"),
                entry.Value))
            .ToDictionary(
                static entry => entry.Key,
                static entry => entry.Value,
                StringComparer.Ordinal);
        _destinationConvention = destinationConvention;
    }

    public ServiceBusEventDestinationDiagnostic DescribeDestination(
        string messageTypeName)
    {
        string normalizedMessageTypeName = messageTypeName.NormalizeRequired(
            nameof(messageTypeName),
            "Message type name");

        if (_destinationsByMessageTypeName.TryGetValue(
            normalizedMessageTypeName,
            out ServiceBusEventDestination? destination))
        {
            return new ServiceBusEventDestinationDiagnostic(
                normalizedMessageTypeName,
                GetExplicitSource(destination.Kind),
                destination);
        }

        if (_destinationConvention is not null)
        {
            return new ServiceBusEventDestinationDiagnostic(
                normalizedMessageTypeName,
                GetConventionSource(_destinationConvention.Kind),
                new ServiceBusEventDestination(
                    _destinationConvention.Kind,
                    _destinationConvention.NameFactory(normalizedMessageTypeName).NormalizeRequired(
                        nameof(_destinationConvention),
                        "Service Bus event destination entity name")));
        }

        return new ServiceBusEventDestinationDiagnostic(
            normalizedMessageTypeName,
            ServiceBusEventDestinationSource.Missing,
            destination: null,
            failureReason:
                $"No Service Bus event destination is configured for message type '{normalizedMessageTypeName}'.");
    }

    private static ServiceBusEventDestinationSource GetExplicitSource(
        ServiceBusEventDestinationKind kind)
    {
        return kind == ServiceBusEventDestinationKind.Queue
            ? ServiceBusEventDestinationSource.ExplicitQueue
            : ServiceBusEventDestinationSource.ExplicitTopic;
    }

    private static ServiceBusEventDestinationSource GetConventionSource(
        ServiceBusEventDestinationKind kind)
    {
        return kind == ServiceBusEventDestinationKind.Queue
            ? ServiceBusEventDestinationSource.QueueConvention
            : ServiceBusEventDestinationSource.TopicConvention;
    }
}
