using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Transport.RabbitMq.Inbox;

namespace Bondstone.Transport.RabbitMq.Outbox;

internal sealed class RabbitMqTopologyConfigurationValidator(
    RabbitMqCommandRoutingTopology commandTopology,
    RabbitMqEventRoutingTopology eventTopology,
    RabbitMqReceiveTopology receiveTopology)
    : IBondstoneConfigurationValidator,
        IDurableTransportTopologyDiagnosticSource
{
    public string TransportName => "RabbitMq";

    public DurableTransportTopologyRouteDiagnostic DescribeCommandRoute(
        string targetModule)
    {
        RabbitMqCommandRoutingDiagnostic diagnostic =
            commandTopology.DescribeRoute(targetModule);
        return new DurableTransportTopologyRouteDiagnostic(
            TransportName,
            MessageKind.Command,
            diagnostic.TargetModule,
            diagnostic.HasRoute,
            diagnostic.FailureReason);
    }

    public DurableTransportTopologyRouteDiagnostic DescribeEventRoute(
        string messageTypeName)
    {
        RabbitMqEventRoutingDiagnostic diagnostic =
            eventTopology.DescribeRoute(messageTypeName);
        return new DurableTransportTopologyRouteDiagnostic(
            TransportName,
            MessageKind.Event,
            diagnostic.MessageTypeName,
            diagnostic.HasRoute,
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
                    $"RabbitMQ transport has no receive binding for event subscriber '{subscriber.SubscriberIdentity}' in module '{subscriber.ModuleName}' for message type '{subscriber.MessageTypeName}'. Configure ReceiveQueue(...).SubscribeEvent(...).");
            }
        }
    }

    private void ValidateReceiveBindings(BondstoneConfigurationValidationContext context)
    {
        foreach (string queueName in receiveTopology.QueueNames)
        {
            RabbitMqReceiveQueueDiagnostic diagnostic =
                receiveTopology.DescribeQueue(queueName);

            foreach (string moduleName in diagnostic.AcceptedModules)
            {
                if (!context.ModuleHasDurableCommandHandlers(moduleName))
                {
                    throw new InvalidOperationException(
                        $"RabbitMQ receive queue '{queueName}' accepts module '{moduleName}', but that module has no registered durable command handlers.");
                }
            }

            foreach (RabbitMqReceiveQueueEventSubscriptionDiagnostic subscription
                in diagnostic.EventSubscriptions)
            {
                if (!context.HasEventSubscriber(
                        subscription.SubscriberModule,
                        subscription.MessageTypeName,
                        subscription.SubscriberIdentity))
                {
                    throw new InvalidOperationException(
                        $"RabbitMQ receive queue '{queueName}' subscribes '{subscription.SubscriberModule}' subscriber '{subscription.SubscriberIdentity}' to event '{subscription.MessageTypeName}', but no matching Bondstone event subscriber is registered.");
                }
            }
        }
    }

    private bool HasReceiveSubscription(
        string messageTypeName,
        string subscriberModule,
        string subscriberIdentity)
    {
        return receiveTopology.QueueNames
            .Select(receiveTopology.DescribeQueue)
            .SelectMany(static queue => queue.EventSubscriptions)
            .Any(subscription =>
                subscription.MessageTypeName == messageTypeName
                && subscription.SubscriberModule == subscriberModule
                && subscription.SubscriberIdentity == subscriberIdentity);
    }
}
