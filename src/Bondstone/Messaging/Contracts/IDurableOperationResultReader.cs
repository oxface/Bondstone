namespace Bondstone.Messaging;

/// <summary>
/// Reads typed durable operation results from operation state.
/// </summary>
public interface IDurableOperationResultReader
{
    /// <summary>
    /// Reads the current state of a durable operation once and deserializes a completed result payload when available.
    /// </summary>
    /// <typeparam name="TResult">The expected result payload type.</typeparam>
    /// <param name="durableOperationId">The durable operation identifier returned or supplied when the command was sent.</param>
    /// <param name="ct">A cancellation token for the read operation.</param>
    /// <returns>The current durable operation result state.</returns>
    ValueTask<DurableOperationResult<TResult>> GetResultAsync<TResult>(
        Guid durableOperationId,
        CancellationToken ct = default);

    /// <summary>
    /// Reads the current state of a durable operation from one hinted module and deserializes a completed result payload when available.
    /// </summary>
    /// <typeparam name="TResult">The expected result payload type.</typeparam>
    /// <param name="durableOperationId">The durable operation identifier returned or supplied when the command was sent.</param>
    /// <param name="moduleName">The module whose operation-state store should be queried.</param>
    /// <param name="ct">A cancellation token for the read operation.</param>
    /// <returns>The current durable operation result state.</returns>
    ValueTask<DurableOperationResult<TResult>> GetResultAsync<TResult>(
        Guid durableOperationId,
        string moduleName,
        CancellationToken ct = default)
    {
        return GetResultAsync<TResult>(durableOperationId, ct);
    }

    /// <summary>
    /// Polls operation state until the durable operation reaches a terminal state or the timeout expires.
    /// </summary>
    /// <typeparam name="TResult">The expected result payload type.</typeparam>
    /// <param name="durableOperationId">The durable operation identifier returned or supplied when the command was sent.</param>
    /// <param name="timeout">The maximum time to wait for a terminal operation state.</param>
    /// <param name="pollInterval">An optional polling interval. When omitted, the reader uses its default interval.</param>
    /// <param name="ct">A cancellation token for the wait operation.</param>
    /// <returns>The terminal durable operation result state, or a timeout result if the operation did not complete in time.</returns>
    ValueTask<DurableOperationResult<TResult>> WaitForResultAsync<TResult>(
        Guid durableOperationId,
        TimeSpan timeout,
        TimeSpan? pollInterval = null,
        CancellationToken ct = default);

    /// <summary>
    /// Polls one hinted module's operation state until the durable operation reaches a terminal state or the timeout expires.
    /// </summary>
    /// <typeparam name="TResult">The expected result payload type.</typeparam>
    /// <param name="durableOperationId">The durable operation identifier returned or supplied when the command was sent.</param>
    /// <param name="moduleName">The module whose operation-state store should be queried.</param>
    /// <param name="timeout">The maximum time to wait for a terminal operation state.</param>
    /// <param name="pollInterval">An optional polling interval. When omitted, the reader uses its default interval.</param>
    /// <param name="ct">A cancellation token for the wait operation.</param>
    /// <returns>The terminal durable operation result state, or a timeout result if the operation did not complete in time.</returns>
    ValueTask<DurableOperationResult<TResult>> WaitForResultAsync<TResult>(
        Guid durableOperationId,
        string moduleName,
        TimeSpan timeout,
        TimeSpan? pollInterval = null,
        CancellationToken ct = default)
    {
        return WaitForResultAsync<TResult>(
            durableOperationId,
            timeout,
            pollInterval,
            ct);
    }
}
