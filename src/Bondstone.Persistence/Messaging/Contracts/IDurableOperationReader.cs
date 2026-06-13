namespace Bondstone.Messaging;

public interface IDurableOperationReader
{
    ValueTask<DurableOperationState?> GetStateAsync(
        Guid durableOperationId,
        CancellationToken ct = default);
}
