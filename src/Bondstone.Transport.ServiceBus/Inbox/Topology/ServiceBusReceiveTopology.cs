using System.Diagnostics.CodeAnalysis;
using Bondstone.Messaging;
using Bondstone.Utility;

namespace Bondstone.Transport.ServiceBus.Inbox;

internal sealed class ServiceBusReceiveTopology
{
    private readonly IReadOnlyDictionary<string, ServiceBusReceiveSourceRegistration> _sources;

    public ServiceBusReceiveTopology(
        IReadOnlyDictionary<string, ServiceBusReceiveSourceRegistration> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        _sources = sources.ToDictionary(
            static entry => entry.Key.NormalizeRequired(
                "sourceKey",
                "Service Bus receive source key"),
            static entry => entry.Value,
            StringComparer.Ordinal);
    }

    public ServiceBusReceiveSourceDiagnostic DescribeSource(
        ServiceBusReceiveSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (!_sources.TryGetValue(
            source.Key,
            out ServiceBusReceiveSourceRegistration? registration))
        {
            return new ServiceBusReceiveSourceDiagnostic(
                source,
                [],
                [],
                $"Service Bus receive source '{source.DisplayName}' is not bound to any Bondstone receive handlers.");
        }

        return new ServiceBusReceiveSourceDiagnostic(
            registration.Source,
            registration.AcceptedModules,
            registration.EventSubscriptions
                .Select(static subscription =>
                    new ServiceBusReceiveSourceEventSubscriptionDiagnostic(
                        subscription.MessageTypeName,
                        subscription.SubscriberModule,
                        subscription.SubscriberIdentity))
                .ToArray());
    }

    public bool AcceptsCommand(
        ServiceBusReceiveSource source,
        DurableMessageEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(envelope);

        if (envelope.MessageKind != MessageKind.Command)
        {
            return false;
        }

        string? targetModule = envelope.TargetModule.NormalizeOptional();
        if (targetModule is null
            || !TryGetSource(source, out ServiceBusReceiveSourceRegistration? registration))
        {
            return false;
        }

        return registration.AcceptedModules.Contains(targetModule);
    }

    public IReadOnlyCollection<ServiceBusEventSubscriptionBinding> GetEventSubscriptions(
        ServiceBusReceiveSource source,
        DurableMessageEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(envelope);

        if (envelope.MessageKind != MessageKind.Event)
        {
            return [];
        }

        if (!TryGetSource(source, out ServiceBusReceiveSourceRegistration? registration))
        {
            return [];
        }

        return registration.EventSubscriptions
            .Where(subscription => subscription.MessageTypeName == envelope.MessageTypeName)
            .ToArray();
    }

    private bool TryGetSource(
        ServiceBusReceiveSource source,
        [NotNullWhen(true)]
        out ServiceBusReceiveSourceRegistration? registration)
    {
        return _sources.TryGetValue(source.Key, out registration);
    }
}
