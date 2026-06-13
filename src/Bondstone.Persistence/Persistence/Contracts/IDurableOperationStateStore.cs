using Bondstone.Messaging;

namespace Bondstone.Persistence;

public interface IDurableOperationStateStore : IDurableOperationReader
{
    ValueTask SaveAsync(
        DurableOperationState state,
        CancellationToken ct = default);
}
