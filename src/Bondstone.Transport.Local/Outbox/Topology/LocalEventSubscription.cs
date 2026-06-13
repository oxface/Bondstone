namespace Bondstone.Transport.Local.Outbox;

internal sealed record LocalEventSubscription(
    string QueueName,
    string MessageTypeName,
    string SubscriberModule,
    string SubscriberIdentity);
