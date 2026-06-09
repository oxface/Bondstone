using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Outbox;
using Bondstone.Utility;

namespace Bondstone.Transport.Rebus.Inbox;

public interface IRebusModuleEventEndpointDispatcher
{
    ValueTask<IReadOnlyCollection<DurableInboxHandleResult>> DispatchAsync(
        string endpointName,
        RebusDurableMessageEnvelope envelope,
        CancellationToken ct = default);
}

internal sealed class RebusModuleEventEndpointDispatcher(
    IRebusEventSubscriptionRegistry subscriptionRegistry,
    IRebusModuleEventReceivePipeline receivePipeline)
    : IRebusModuleEventEndpointDispatcher
{
    private readonly IRebusEventSubscriptionRegistry _subscriptionRegistry =
        subscriptionRegistry ?? throw new ArgumentNullException(nameof(subscriptionRegistry));
    private readonly IRebusModuleEventReceivePipeline _receivePipeline =
        receivePipeline ?? throw new ArgumentNullException(nameof(receivePipeline));

    public async ValueTask<IReadOnlyCollection<DurableInboxHandleResult>> DispatchAsync(
        string endpointName,
        RebusDurableMessageEnvelope envelope,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(envelope);

        string normalizedEndpointName = endpointName.NormalizeRequired(
            nameof(endpointName),
            "Rebus receive endpoint name");

        IReadOnlyCollection<RebusEventSubscriptionBinding> subscriptions =
            _subscriptionRegistry.GetSubscriptions(
                normalizedEndpointName,
                envelope.MessageTypeName);

        var results = new List<DurableInboxHandleResult>(subscriptions.Count);
        foreach (RebusEventSubscriptionBinding subscription in subscriptions)
        {
            DurableInboxHandleResult result = await _receivePipeline.HandleOnceAsync(
                envelope,
                subscription.SubscriberModule,
                subscription.SubscriberIdentity,
                ct);

            results.Add(result);
        }

        return results;
    }
}
