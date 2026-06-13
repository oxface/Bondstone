using Bondstone.Utility;

namespace Bondstone.Transport.ServiceBus.Outbox;

internal sealed class ServiceBusCommandDestinationTopology
{
    private readonly IReadOnlyDictionary<string, string> _queueNamesByTargetModule;
    private readonly Func<string, string>? _queueNameConvention;

    public ServiceBusCommandDestinationTopology(
        IReadOnlyDictionary<string, string> queueNamesByTargetModule,
        Func<string, string>? queueNameConvention = null)
    {
        ArgumentNullException.ThrowIfNull(queueNamesByTargetModule);

        _queueNamesByTargetModule = queueNamesByTargetModule
            .Select(static entry => new KeyValuePair<string, string>(
                entry.Key.NormalizeRequired("targetModule", "Target module"),
                entry.Value.NormalizeRequired("queueName", "Service Bus queue name")))
            .ToDictionary(
                static entry => entry.Key,
                static entry => entry.Value,
                StringComparer.Ordinal);
        _queueNameConvention = queueNameConvention;
    }

    public ServiceBusCommandDestinationDiagnostic DescribeDestination(
        string targetModule)
    {
        string normalizedTargetModule = targetModule.NormalizeRequired(
            nameof(targetModule),
            "Target module");

        if (_queueNamesByTargetModule.TryGetValue(
            normalizedTargetModule,
            out string? queueName))
        {
            return new ServiceBusCommandDestinationDiagnostic(
                normalizedTargetModule,
                ServiceBusCommandDestinationSource.ExplicitQueue,
                queueName);
        }

        if (_queueNameConvention is not null)
        {
            return new ServiceBusCommandDestinationDiagnostic(
                normalizedTargetModule,
                ServiceBusCommandDestinationSource.QueueConvention,
                _queueNameConvention(normalizedTargetModule).NormalizeRequired(
                    nameof(_queueNameConvention),
                    "Service Bus queue name"));
        }

        return new ServiceBusCommandDestinationDiagnostic(
            normalizedTargetModule,
            ServiceBusCommandDestinationSource.Missing,
            failureReason:
                $"No Service Bus queue is configured for target module '{normalizedTargetModule}'.");
    }
}
