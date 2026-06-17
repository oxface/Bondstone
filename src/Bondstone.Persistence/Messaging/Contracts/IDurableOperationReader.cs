namespace Bondstone.Messaging;

/// <summary>
/// Reads durable operation state persisted by module-owned operation stores.
/// </summary>
/// <remarks>
/// Operation state is the caller-visible workflow/result read model. It is not
/// the outbox ledger, inbox ledger, broker retry state, or dead-letter state.
/// Applications should write explicit terminal outcomes through the operation
/// finalizer API when policy decides an operation is failed or cancelled.
/// </remarks>
public interface IDurableOperationReader
{
    /// <summary>
    /// Reads an operation state by durable operation id across the configured operation-state stores.
    /// </summary>
    /// <remarks>
    /// This aggregate read may query multiple configured module stores. Prefer
    /// a module-hinted read or <see cref="DurableOperationHandle"/> when the
    /// target module is known.
    /// </remarks>
    /// <param name="durableOperationId">The durable operation identifier returned or supplied when the command was sent.</param>
    /// <param name="ct">A cancellation token for the read operation.</param>
    /// <returns>The operation state when found; otherwise, <see langword="null"/>.</returns>
    ValueTask<DurableOperationState?> GetStateAsync(
        Guid durableOperationId,
        CancellationToken ct = default);

    /// <summary>
    /// Reads an operation state by durable operation id from one hinted module operation-state store.
    /// </summary>
    /// <remarks>
    /// The module hint should name the module that owns the operation-state
    /// store for the expected result.
    /// </remarks>
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
    /// <remarks>
    /// This is the preferred read path after sending a durable command with an
    /// operation id, because the handle names the target module that owns the
    /// completed operation state.
    /// </remarks>
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
