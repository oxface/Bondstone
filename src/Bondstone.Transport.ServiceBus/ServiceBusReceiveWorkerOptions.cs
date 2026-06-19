using Azure.Messaging.ServiceBus;
using Bondstone.Messaging;

namespace Bondstone.Transport.ServiceBus;

public sealed class ServiceBusReceiveWorkerOptions
{
    public string? QueueName { get; set; }

    public string? TopicName { get; set; }

    public string? SubscriptionName { get; set; }

    public DurableEnvelopeReceiveBinding? Binding { get; private set; }

    public ServiceBusProcessorOptions ProcessorOptions { get; } =
        new()
        {
            AutoCompleteMessages = false,
        };

    public ServiceBusReceiveWorkerOptions ReceiveCommand()
    {
        Binding = null;
        return this;
    }

    public ServiceBusReceiveWorkerOptions ReceiveEvent(
        string subscriberModule,
        string subscriberIdentity)
    {
        Binding = new DurableEnvelopeReceiveBinding(
            subscriberModule,
            subscriberIdentity);
        return this;
    }

    internal ServiceBusReceiveWorkerRegistration ToRegistration()
    {
        bool isQueue = !string.IsNullOrWhiteSpace(QueueName);
        bool isSubscription =
            !string.IsNullOrWhiteSpace(TopicName)
            && !string.IsNullOrWhiteSpace(SubscriptionName);

        if (isQueue == isSubscription)
        {
            throw new InvalidOperationException(
                "Service Bus receive worker requires either QueueName or TopicName plus SubscriptionName.");
        }

        return new ServiceBusReceiveWorkerRegistration(
            QueueName,
            TopicName,
            SubscriptionName,
            Binding,
            CloneManualCompletionProcessorOptions(ProcessorOptions),
            isQueue
                ? $"servicebus:{QueueName!.Trim()}"
                : $"servicebus:{TopicName!.Trim()}/{SubscriptionName!.Trim()}");
    }

    internal static ServiceBusProcessorOptions CloneManualCompletionProcessorOptions(
        ServiceBusProcessorOptions processorOptions)
    {
        ArgumentNullException.ThrowIfNull(processorOptions);

        var clone = new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = processorOptions.AutoCompleteMessages,
            Identifier = processorOptions.Identifier,
            MaxAutoLockRenewalDuration = processorOptions.MaxAutoLockRenewalDuration,
            MaxConcurrentCalls = processorOptions.MaxConcurrentCalls,
            PrefetchCount = processorOptions.PrefetchCount,
            ReceiveMode = processorOptions.ReceiveMode,
            SubQueue = processorOptions.SubQueue,
        };

        if (clone.AutoCompleteMessages)
        {
            throw new InvalidOperationException(
                "Service Bus receive worker requires ProcessorOptions.AutoCompleteMessages to be false so Bondstone can complete messages after durable receive succeeds.");
        }

        return clone;
    }
}
