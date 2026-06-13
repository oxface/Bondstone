using Bondstone.Utility;

namespace Bondstone.Transport.RabbitMq.Inbox;

public sealed class RabbitMqReceiveQueueEventSubscriptionDiagnostic
{
    public RabbitMqReceiveQueueEventSubscriptionDiagnostic(
        string messageTypeName,
        string subscriberModule,
        string subscriberIdentity)
    {
        MessageTypeName = messageTypeName.NormalizeRequired(
            nameof(messageTypeName),
            "Message type name");
        SubscriberModule = subscriberModule.NormalizeRequired(
            nameof(subscriberModule),
            "Subscriber module");
        SubscriberIdentity = subscriberIdentity.NormalizeRequired(
            nameof(subscriberIdentity),
            "Subscriber identity");
    }

    public string MessageTypeName { get; }

    public string SubscriberModule { get; }

    public string SubscriberIdentity { get; }
}
