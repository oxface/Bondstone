namespace Bondstone.Messaging;

public interface IDurableOperationReader
{
    ValueTask<DurableOperationState?> GetStateAsync(
        Guid durableOperationId,
        CancellationToken ct = default);

    ValueTask<DurableOperationState?> GetStateAsync(
        Guid durableOperationId,
        string moduleName,
        CancellationToken ct = default)
    {
        return GetStateAsync(durableOperationId, ct);
    }
}
