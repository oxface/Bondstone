using Bondstone.Utility;

namespace Bondstone.Transport.RabbitMq.Outbox;

internal sealed class RabbitMqCommandRoutingDiagnostic
{
    public RabbitMqCommandRoutingDiagnostic(
        string targetModule,
        RabbitMqCommandRoutingSource source,
        string? exchangeName,
        string? routingKey,
        string? failureReason = null)
    {
        TargetModule = targetModule.NormalizeRequired(
            nameof(targetModule),
            "Target module");
        Source = source;
        ExchangeName = exchangeName?.NormalizeRequired(
            nameof(exchangeName),
            "RabbitMQ exchange name");
        RoutingKey = routingKey?.NormalizeRequired(
            nameof(routingKey),
            "RabbitMQ routing key");
        FailureReason = failureReason;
    }

    public string TargetModule { get; }

    public RabbitMqCommandRoutingSource Source { get; }

    public string? ExchangeName { get; }

    public string? RoutingKey { get; }

    public string? FailureReason { get; }

    public bool HasRoute => ExchangeName is not null && RoutingKey is not null;
}
