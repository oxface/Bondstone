using Azure.Messaging.ServiceBus;
using Bondstone.Messaging;

namespace Bondstone.Transport.ServiceBus;

internal sealed record ServiceBusReceiveWorkerRegistration(
    string? QueueName,
    string? TopicName,
    string? SubscriptionName,
    DurableEnvelopeReceiveBinding? Binding,
    ServiceBusProcessorOptions ProcessorOptions,
    string SourceTransportName);
