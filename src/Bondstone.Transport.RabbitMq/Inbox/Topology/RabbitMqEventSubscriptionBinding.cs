namespace Bondstone.Transport.RabbitMq.Inbox;

internal sealed record RabbitMqEventSubscriptionBinding(
    string QueueName,
    string MessageTypeName,
    string SubscriberModule,
    string SubscriberIdentity);
