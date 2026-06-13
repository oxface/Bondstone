using Bondstone.Messaging;
using Bondstone.Persistence;

namespace Bondstone.Modules;

public interface IModuleEventReceivePipeline
{
    ValueTask<DurableInboxHandleResult> HandleOnceAsync(
        DurableMessageEnvelope envelope,
        string subscriberModule,
        string subscriberIdentity,
        CancellationToken ct = default);
}
