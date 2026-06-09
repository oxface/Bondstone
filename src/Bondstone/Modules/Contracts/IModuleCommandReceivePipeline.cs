using Bondstone.Messaging;
using Bondstone.Persistence;

namespace Bondstone.Modules;

public interface IModuleCommandReceivePipeline
{
    ValueTask<DurableInboxHandleResult> HandleOnceAsync(
        DurableMessageEnvelope envelope,
        CancellationToken ct = default);
}
