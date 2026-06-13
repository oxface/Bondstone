using Bondstone.Utility;

namespace Bondstone.Transport.RabbitMq.Outbox;

internal sealed class RabbitMqCommandRoutingTopology
{
    private readonly IReadOnlyDictionary<string, string> _routingKeysByTargetModule;
    private readonly Func<string, string>? _routingKeyConvention;

    public RabbitMqCommandRoutingTopology(
        string? exchangeName,
        IReadOnlyDictionary<string, string> routingKeysByTargetModule,
        Func<string, string>? routingKeyConvention = null)
    {
        ArgumentNullException.ThrowIfNull(routingKeysByTargetModule);

        ExchangeName = exchangeName.NormalizeOptional();
        _routingKeysByTargetModule = routingKeysByTargetModule
            .Select(static entry => new KeyValuePair<string, string>(
                entry.Key.NormalizeRequired("targetModule", "Target module"),
                entry.Value.NormalizeRequired("routingKey", "RabbitMQ routing key")))
            .ToDictionary(
                static entry => entry.Key,
                static entry => entry.Value,
                StringComparer.Ordinal);
        _routingKeyConvention = routingKeyConvention;
    }

    public string? ExchangeName { get; }

    public RabbitMqCommandRoutingDiagnostic DescribeRoute(
        string targetModule)
    {
        string normalizedTargetModule = targetModule.NormalizeRequired(
            nameof(targetModule),
            "Target module");

        if (ExchangeName is null)
        {
            return new RabbitMqCommandRoutingDiagnostic(
                normalizedTargetModule,
                RabbitMqCommandRoutingSource.Missing,
                exchangeName: null,
                routingKey: null,
                failureReason: "No RabbitMQ command exchange is configured.");
        }

        if (_routingKeysByTargetModule.TryGetValue(
            normalizedTargetModule,
            out string? routingKey))
        {
            return new RabbitMqCommandRoutingDiagnostic(
                normalizedTargetModule,
                RabbitMqCommandRoutingSource.ExplicitRoutingKey,
                ExchangeName,
                routingKey);
        }

        if (_routingKeyConvention is not null)
        {
            return new RabbitMqCommandRoutingDiagnostic(
                normalizedTargetModule,
                RabbitMqCommandRoutingSource.RoutingKeyConvention,
                ExchangeName,
                _routingKeyConvention(normalizedTargetModule).NormalizeRequired(
                    nameof(_routingKeyConvention),
                    "RabbitMQ routing key"));
        }

        return new RabbitMqCommandRoutingDiagnostic(
            normalizedTargetModule,
            RabbitMqCommandRoutingSource.Missing,
            ExchangeName,
            routingKey: null,
            failureReason:
                $"No RabbitMQ routing key is configured for target module '{normalizedTargetModule}'.");
    }
}
