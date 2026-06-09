using Bondstone.Utility;

namespace Bondstone.Transport.RabbitMq.Outbox;

internal sealed class RabbitMqEventRoutingTopology
{
    private readonly IReadOnlyDictionary<string, RabbitMqEventRouteRegistration> _routesByMessageTypeName;
    private readonly RabbitMqEventDestinationConvention? _destinationConvention;

    public RabbitMqEventRoutingTopology(
        string? exchangeName,
        IReadOnlyDictionary<string, RabbitMqEventRouteRegistration> routesByMessageTypeName,
        RabbitMqEventDestinationConvention? destinationConvention = null)
    {
        ArgumentNullException.ThrowIfNull(routesByMessageTypeName);

        ExchangeName = exchangeName.NormalizeOptional();
        _routesByMessageTypeName = routesByMessageTypeName
            .Select(static entry => new KeyValuePair<string, RabbitMqEventRouteRegistration>(
                entry.Key.NormalizeRequired("messageTypeName", "Message type name"),
                entry.Value))
            .ToDictionary(
                static entry => entry.Key,
                static entry => entry.Value,
                StringComparer.Ordinal);
        _destinationConvention = destinationConvention;
    }

    public string? ExchangeName { get; }

    public RabbitMqEventRoutingDiagnostic DescribeRoute(
        string messageTypeName)
    {
        string normalizedMessageTypeName = messageTypeName.NormalizeRequired(
            nameof(messageTypeName),
            "Message type name");

        if (_routesByMessageTypeName.TryGetValue(
            normalizedMessageTypeName,
            out RabbitMqEventRouteRegistration? route))
        {
            RabbitMqPublishDestination? destination = CreateDestination(route);
            if (destination is null)
            {
                return MissingExchange(normalizedMessageTypeName);
            }

            return new RabbitMqEventRoutingDiagnostic(
                normalizedMessageTypeName,
                GetExplicitSource(destination.Kind),
                destination);
        }

        if (_destinationConvention is not null)
        {
            var conventionRoute = new RabbitMqEventRouteRegistration(
                _destinationConvention.Kind,
                _destinationConvention.DestinationNameFactory(normalizedMessageTypeName).NormalizeRequired(
                    nameof(_destinationConvention),
                    "RabbitMQ event destination name"));
            RabbitMqPublishDestination? destination = CreateDestination(conventionRoute);
            if (destination is null)
            {
                return MissingExchange(normalizedMessageTypeName);
            }

            return new RabbitMqEventRoutingDiagnostic(
                normalizedMessageTypeName,
                GetConventionSource(_destinationConvention.Kind),
                destination);
        }

        if (ExchangeName is null)
        {
            return MissingExchange(normalizedMessageTypeName);
        }

        return new RabbitMqEventRoutingDiagnostic(
            normalizedMessageTypeName,
            RabbitMqEventRoutingSource.Missing,
            destination: null,
            failureReason:
                $"No RabbitMQ routing key is configured for message type '{normalizedMessageTypeName}'.");
    }

    private RabbitMqPublishDestination? CreateDestination(
        RabbitMqEventRouteRegistration route)
    {
        if (route.Kind == RabbitMqPublishDestinationKind.Queue)
        {
            return RabbitMqPublishDestination.ForQueue(route.DestinationName);
        }

        return ExchangeName is null
            ? null
            : new RabbitMqPublishDestination(ExchangeName, route.DestinationName);
    }

    private static RabbitMqEventRoutingDiagnostic MissingExchange(
        string messageTypeName)
    {
        return new RabbitMqEventRoutingDiagnostic(
            messageTypeName,
            RabbitMqEventRoutingSource.Missing,
            destination: null,
            failureReason: "No RabbitMQ event exchange is configured.");
    }

    private static RabbitMqEventRoutingSource GetExplicitSource(
        RabbitMqPublishDestinationKind kind)
    {
        return kind == RabbitMqPublishDestinationKind.Queue
            ? RabbitMqEventRoutingSource.ExplicitQueue
            : RabbitMqEventRoutingSource.ExplicitRoutingKey;
    }

    private static RabbitMqEventRoutingSource GetConventionSource(
        RabbitMqPublishDestinationKind kind)
    {
        return kind == RabbitMqPublishDestinationKind.Queue
            ? RabbitMqEventRoutingSource.QueueConvention
            : RabbitMqEventRoutingSource.RoutingKeyConvention;
    }
}
