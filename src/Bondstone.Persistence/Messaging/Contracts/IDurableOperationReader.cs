namespace Bondstone.Messaging;

/// <summary>
/// Reads durable operation state persisted by module-owned operation stores.
/// </summary>
public interface IDurableOperationReader
{
    /// <summary>
    /// Reads an operation state by durable operation id across the configured operation-state stores.
    /// </summary>
    /// <param name="durableOperationId">The durable operation identifier returned or supplied when the command was sent.</param>
    /// <param name="ct">A cancellation token for the read operation.</param>
    /// <returns>The operation state when found; otherwise, <see langword="null"/>.</returns>
    ValueTask<DurableOperationState?> GetStateAsync(
        Guid durableOperationId,
        CancellationToken ct = default);

    /// <summary>
    /// Reads an operation state by durable operation id from one hinted module operation-state store.
    /// </summary>
    /// <param name="durableOperationId">The durable operation identifier returned or supplied when the command was sent.</param>
    /// <param name="moduleName">The module whose operation-state store should be queried.</param>
    /// <param name="ct">A cancellation token for the read operation.</param>
    /// <returns>The operation state when found; otherwise, <see langword="null"/>.</returns>
    ValueTask<DurableOperationState?> GetStateAsync(
        Guid durableOperationId,
        string moduleName,
        CancellationToken ct = default)
    {
        return GetStateAsync(durableOperationId, ct);
    }

    /// <summary>
    /// Reads an operation state using the target-module hint carried by a durable operation handle.
    /// </summary>
    /// <param name="operation">The durable operation handle returned when the command was sent.</param>
    /// <param name="ct">A cancellation token for the read operation.</param>
    /// <returns>The operation state when found; otherwise, <see langword="null"/>.</returns>
    ValueTask<DurableOperationState?> GetStateAsync(
        DurableOperationHandle operation,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return GetStateAsync(
            operation.DurableOperationId,
            operation.TargetModule,
            ct);
    }
}
