using Bondstone.Messaging;
using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Outbox;

namespace Bondstone.Transport.Rebus.Inbox;

public interface IRebusDurableInboxHandlerExecutor
{
    ValueTask<DurableInboxHandleResult> HandleOnceAsync(
        RebusDurableMessageEnvelope envelope,
        string handlerIdentity,
        Func<DurableMessageEnvelope, CancellationToken, ValueTask> handler,
        Func<CancellationToken, ValueTask> commit,
        CancellationToken ct = default);
}
