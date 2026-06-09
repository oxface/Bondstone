using Bondstone.Utility;

namespace Bondstone.Transport.Rebus.Outbox;

public sealed class RebusEventSubscriberDiagnostic
{
    public RebusEventSubscriberDiagnostic(
        string endpointName,
        string subscriberModule,
        string subscriberIdentity)
    {
        EndpointName = endpointName.NormalizeRequired(
            nameof(endpointName),
            "Rebus receive endpoint name");
        SubscriberModule = subscriberModule.NormalizeRequired(
            nameof(subscriberModule),
            "Subscriber module");
        SubscriberIdentity = subscriberIdentity.NormalizeRequired(
            nameof(subscriberIdentity),
            "Subscriber identity");
    }

    public string EndpointName { get; }

    public string SubscriberModule { get; }

    public string SubscriberIdentity { get; }
}
