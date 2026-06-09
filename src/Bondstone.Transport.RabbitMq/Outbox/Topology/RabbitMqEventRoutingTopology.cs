using Bondstone.Utility;

namespace Bondstone.Transport.RabbitMq.Outbox;

internal sealed class RabbitMqEventRoutingTopology
{
    private readonly IReadOnlyDictionary<string, string> _routingKeysByMessageTypeName;
    private readonly Func<string, string>? _routingKeyConvention;

    public RabbitMqEventRoutingTopology(
        string? exchangeName,
        IReadOnlyDictionary<string, string> routingKeysByMessageTypeName,
        Func<string, string>? routingKeyConvention = null)
    {
        ArgumentNullException.ThrowIfNull(routingKeysByMessageTypeName);

        ExchangeName = exchangeName.NormalizeOptional();
        _routingKeysByMessageTypeName = routingKeysByMessageTypeName
            .Select(static entry => new KeyValuePair<string, string>(
                entry.Key.NormalizeRequired("messageTypeName", "Message type name"),
                entry.Value.NormalizeRequired("routingKey", "RabbitMQ routing key")))
            .ToDictionary(
                static entry => entry.Key,
                static entry => entry.Value,
                StringComparer.Ordinal);
        _routingKeyConvention = routingKeyConvention;
    }

    public string? ExchangeName { get; }

    public RabbitMqEventRoutingDiagnostic DescribeRoute(
        string messageTypeName)
    {
        string normalizedMessageTypeName = messageTypeName.NormalizeRequired(
            nameof(messageTypeName),
            "Message type name");

        if (ExchangeName is null)
        {
            return new RabbitMqEventRoutingDiagnostic(
                normalizedMessageTypeName,
                RabbitMqEventRoutingSource.Missing,
                exchangeName: null,
                routingKey: null,
                failureReason: "No RabbitMQ event exchange is configured.");
        }

        if (_routingKeysByMessageTypeName.TryGetValue(
            normalizedMessageTypeName,
            out string? routingKey))
        {
            return new RabbitMqEventRoutingDiagnostic(
                normalizedMessageTypeName,
                RabbitMqEventRoutingSource.ExplicitRoutingKey,
                ExchangeName,
                routingKey);
        }

        if (_routingKeyConvention is not null)
        {
            return new RabbitMqEventRoutingDiagnostic(
                normalizedMessageTypeName,
                RabbitMqEventRoutingSource.RoutingKeyConvention,
                ExchangeName,
                _routingKeyConvention(normalizedMessageTypeName).NormalizeRequired(
                    nameof(_routingKeyConvention),
                    "RabbitMQ routing key"));
        }

        return new RabbitMqEventRoutingDiagnostic(
            normalizedMessageTypeName,
            RabbitMqEventRoutingSource.Missing,
            ExchangeName,
            routingKey: null,
            failureReason:
                $"No RabbitMQ routing key is configured for message type '{normalizedMessageTypeName}'.");
    }
}
