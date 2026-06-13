using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Transport.ServiceBus.Inbox;

namespace Bondstone.Transport.ServiceBus.Outbox;

internal sealed class ServiceBusTopologyConfigurationValidator(
    ServiceBusCommandDestinationTopology commandTopology,
    ServiceBusEventDestinationTopology eventTopology,
    ServiceBusReceiveTopology receiveTopology)
    : IBondstoneConfigurationValidator,
        IDurableTransportTopologyDiagnosticSource
{
    public string TransportName => "ServiceBus";

    public DurableTransportTopologyRouteDiagnostic DescribeCommandRoute(
        string targetModule)
    {
        ServiceBusCommandDestinationDiagnostic diagnostic =
            commandTopology.DescribeDestination(targetModule);
        return new DurableTransportTopologyRouteDiagnostic(
            TransportName,
            MessageKind.Command,
            diagnostic.TargetModule,
            diagnostic.HasDestination,
            diagnostic.FailureReason);
    }

    public DurableTransportTopologyRouteDiagnostic DescribeEventRoute(
        string messageTypeName)
    {
        ServiceBusEventDestinationDiagnostic diagnostic =
            eventTopology.DescribeDestination(messageTypeName);
        return new DurableTransportTopologyRouteDiagnostic(
            TransportName,
            MessageKind.Event,
            diagnostic.MessageTypeName,
            diagnostic.HasDestination,
            diagnostic.FailureReason);
    }

    public void Validate(BondstoneConfigurationValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ValidateReceiveBindings(context);

        if (!context.HasSingleTransport)
        {
            return;
        }

        ValidateSubscriberReceiveBindings(context);
        ValidateQueueDestinationReceiveBindings(context);
    }

    private void ValidateSubscriberReceiveBindings(BondstoneConfigurationValidationContext context)
    {
        foreach (ModuleEventSubscriberRegistration subscriber in context.EventSubscribers)
        {
            if (!HasReceiveSubscription(
                    subscriber.MessageTypeName,
                    subscriber.ModuleName,
                    subscriber.SubscriberIdentity))
            {
                throw new InvalidOperationException(
                    $"Service Bus transport has no receive binding for event subscriber '{subscriber.SubscriberIdentity}' in module '{subscriber.ModuleName}' for message type '{subscriber.MessageTypeName}'. Configure ReceiveQueue(...).SubscribeEvent(...) or ReceiveSubscription(...).SubscribeEvent(...).");
            }
        }
    }

    private void ValidateReceiveBindings(BondstoneConfigurationValidationContext context)
    {
        foreach (ServiceBusReceiveSource source in receiveTopology.Sources)
        {
            ServiceBusReceiveSourceDiagnostic diagnostic =
                receiveTopology.DescribeSource(source);

            foreach (string moduleName in diagnostic.AcceptedModules)
            {
                if (!context.ModuleHasDurableCommandHandlers(moduleName))
                {
                    throw new InvalidOperationException(
                        $"Service Bus receive source '{source.DisplayName}' accepts module '{moduleName}', but that module has no registered durable command handlers.");
                }
            }

            foreach (ServiceBusReceiveSourceEventSubscriptionDiagnostic subscription
                in diagnostic.EventSubscriptions)
            {
                if (!context.HasEventSubscriber(
                        subscription.SubscriberModule,
                        subscription.MessageTypeName,
                        subscription.SubscriberIdentity))
                {
                    throw new InvalidOperationException(
                        $"Service Bus receive source '{source.DisplayName}' subscribes '{subscription.SubscriberModule}' subscriber '{subscription.SubscriberIdentity}' to event '{subscription.MessageTypeName}', but no matching Bondstone event subscriber is registered.");
                }
            }
        }
    }

    private bool HasReceiveSubscription(
        string messageTypeName,
        string subscriberModule,
        string subscriberIdentity)
    {
        return receiveTopology.Sources
            .Select(receiveTopology.DescribeSource)
            .SelectMany(static source => source.EventSubscriptions)
            .Any(subscription =>
                subscription.MessageTypeName == messageTypeName
                && subscription.SubscriberModule == subscriberModule
                && subscription.SubscriberIdentity == subscriberIdentity);
    }

    private void ValidateQueueDestinationReceiveBindings(BondstoneConfigurationValidationContext context)
    {
        foreach (string messageTypeName in context.EventSubscribers
            .Select(static subscriber => subscriber.MessageTypeName)
            .Distinct(StringComparer.Ordinal))
        {
            ServiceBusEventDestinationDiagnostic destinationDiagnostic =
                eventTopology.DescribeDestination(messageTypeName);

            if (destinationDiagnostic.DestinationKind != ServiceBusEventDestinationKind.Queue
                || destinationDiagnostic.EntityName is null)
            {
                continue;
            }

            ServiceBusReceiveSource[] receiveSources = receiveTopology.Sources
                .Where(source => receiveTopology.DescribeSource(source)
                    .EventSubscriptions
                    .Any(subscription => subscription.MessageTypeName == messageTypeName))
                .OrderBy(static source => source.DisplayName, StringComparer.Ordinal)
                .ToArray();

            if (receiveSources.Length == 1
                && receiveSources[0].Kind == ServiceBusReceiveSourceKind.Queue
                && StringComparer.Ordinal.Equals(receiveSources[0].EntityName, destinationDiagnostic.EntityName))
            {
                continue;
            }

            throw new InvalidOperationException(
                $"Service Bus event '{messageTypeName}' is routed directly to queue '{destinationDiagnostic.EntityName}', but its receive bindings are configured on {FormatReceiveSources(receiveSources)}. Direct queue event routing supports same-queue in-process fan-out; split subscribers should use a topic with separate subscriptions.");
        }
    }

    private static string FormatReceiveSources(
        IReadOnlyCollection<ServiceBusReceiveSource> sources)
    {
        return sources.Count == 0
            ? "no Service Bus receive sources"
            : $"Service Bus receive source(s): {string.Join(", ", sources.Select(static source => source.DisplayName))}";
    }
}
