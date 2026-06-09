using Bondstone.Messaging;
using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Outbox;

namespace Bondstone.Transport.Rebus.Inbox;

public interface IRebusTypedCommandReceivePipeline
{
    ValueTask<DurableInboxHandleResult> HandleOnceAsync<TCommand>(
        RebusDurableMessageEnvelope envelope,
        string handlerIdentity,
        Func<TCommand, CancellationToken, ValueTask> handler,
        Func<CancellationToken, ValueTask> commit,
        CancellationToken ct = default)
        where TCommand : IDurableCommand;
}
