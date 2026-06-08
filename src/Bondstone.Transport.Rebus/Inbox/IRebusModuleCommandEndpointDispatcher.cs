using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Outbox;

namespace Bondstone.Transport.Rebus.Inbox;

public interface IRebusModuleCommandEndpointDispatcher
{
    ValueTask<DurableInboxHandleResult> DispatchAsync(
        string endpointName,
        RebusDurableMessageEnvelope envelope,
        CancellationToken ct = default);
}
