namespace Bondstone.Transport.ServiceBus.Inbox;

internal sealed record ServiceBusEventSubscriptionBinding(
    ServiceBusReceiveSource Source,
    string MessageTypeName,
    string SubscriberModule,
    string SubscriberIdentity);
