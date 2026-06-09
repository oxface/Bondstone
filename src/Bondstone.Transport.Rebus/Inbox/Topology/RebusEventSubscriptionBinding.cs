using Bondstone.Utility;

namespace Bondstone.Transport.Rebus.Inbox;

public sealed class RebusEventSubscriptionBinding
{
    public RebusEventSubscriptionBinding(
        string endpointName,
        string messageTypeName,
        string subscriberModule,
        string subscriberIdentity)
    {
        EndpointName = endpointName.NormalizeRequired(
            nameof(endpointName),
            "Rebus receive endpoint name");
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

    public string EndpointName { get; }

    public string MessageTypeName { get; }

    public string SubscriberModule { get; }

    public string SubscriberIdentity { get; }
}
