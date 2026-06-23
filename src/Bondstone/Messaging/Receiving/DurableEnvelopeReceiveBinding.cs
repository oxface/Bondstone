namespace Bondstone.Messaging;

public sealed record DurableEnvelopeReceiveBinding(
    string SubscriberModule,
    string SubscriberIdentity);
