namespace Bondstone.Messaging;

/// <summary>
/// Reads typed durable operation results from operation state.
/// </summary>
/// <remarks>
/// Operation result reads observe caller-visible operation state. They do not
/// inspect broker retry, dead-letter queues, source outbox retry state, or
/// receive-side ambiguity unless application code records an explicit terminal
/// operation outcome.
/// </remarks>
public interface IDurableOperationResultReader
{
    /// <summary>
    /// Reads the current state of a durable operation once and deserializes a completed result payload when available.
    /// </summary>
    /// <remarks>
    /// This method performs one read and never waits for the operation to
    /// become terminal. Use the returned
    /// <see cref="DurableOperationResult{TResult}.State"/> to distinguish
    /// unknown, pending, completed, failed, cancelled, and deserialization
    /// failure outcomes.
    /// </remarks>
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
    /// <remarks>
    /// The module hint should name the module that owns the operation-state
    /// store for the result. For durable command sends, prefer the target
    /// module carried by <see cref="DurableOperationHandle"/>.
    /// </remarks>
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
    /// Reads the current state of a durable operation using the target-module hint carried by a durable operation handle.
    /// </summary>
    /// <remarks>
    /// This is the preferred read path when the durable send returned an
    /// operation handle, because it queries the target module that owns the
    /// completed result state.
    /// </remarks>
    /// <typeparam name="TResult">The expected result payload type.</typeparam>
    /// <param name="operation">The durable operation handle returned when the command was sent.</param>
    /// <param name="ct">A cancellation token for the read operation.</param>
    /// <returns>The current durable operation result state.</returns>
    ValueTask<DurableOperationResult<TResult>> GetResultAsync<TResult>(
        DurableOperationHandle operation,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return GetResultAsync<TResult>(
            operation.DurableOperationId,
            operation.TargetModule,
            ct);
    }

    /// <summary>
    /// Polls operation state until the durable operation reaches a terminal state.
    /// </summary>
    /// <remarks>
    /// Timeout is caller patience, not durable operation state. If the timeout
    /// expires before a terminal state is observed, this method throws
    /// <see cref="TimeoutException"/> and does not write <c>Failed</c>,
    /// <c>Cancelled</c>, or any other operation state. Use
    /// <see cref="IDurableOperationFinalizer"/> when application policy should
    /// explicitly finalize an operation.
    /// </remarks>
    /// <typeparam name="TResult">The expected result payload type.</typeparam>
    /// <param name="durableOperationId">The durable operation identifier returned or supplied when the command was sent.</param>
    /// <param name="timeout">The maximum time to wait for a terminal operation state.</param>
    /// <param name="pollInterval">An optional polling interval. When omitted, the reader uses its default interval.</param>
    /// <param name="ct">A cancellation token for the wait operation.</param>
    /// <returns>The terminal durable operation result state.</returns>
    /// <exception cref="TimeoutException">Thrown when the operation does not reach a terminal state before <paramref name="timeout"/> expires.</exception>
    ValueTask<DurableOperationResult<TResult>> WaitForResultAsync<TResult>(
        Guid durableOperationId,
        TimeSpan timeout,
        TimeSpan? pollInterval = null,
        CancellationToken ct = default);

    /// <summary>
    /// Polls operation state until the durable operation reaches a terminal state or the caller's timeout expires.
    /// </summary>
    /// <remarks>
    /// Timeout is caller patience, not durable operation state. If the timeout
    /// expires before a terminal state is observed, this method returns
    /// <see cref="DurableOperationWaitResult{TResult}.CompletedWithinTimeout"/>
    /// as <see langword="false"/> with the latest observed operation result
    /// and does not write <c>Failed</c>, <c>Cancelled</c>, or any other
    /// operation state.
    /// </remarks>
    /// <typeparam name="TResult">The expected result payload type.</typeparam>
    /// <param name="durableOperationId">The durable operation identifier returned or supplied when the command was sent.</param>
    /// <param name="timeout">The maximum time to wait for a terminal operation state.</param>
    /// <param name="pollInterval">An optional polling interval. When omitted, the reader uses its default interval.</param>
    /// <param name="ct">A cancellation token for the wait operation.</param>
    /// <returns>The caller wait outcome and latest durable operation result state.</returns>
    ValueTask<DurableOperationWaitResult<TResult>> TryWaitForResultAsync<TResult>(
        Guid durableOperationId,
        TimeSpan timeout,
        TimeSpan? pollInterval = null,
        CancellationToken ct = default)
    {
        return TryWaitForResultCoreAsync<TResult>(
            durableOperationId,
            timeout,
            pollInterval,
            ct);
    }

    /// <summary>
    /// Polls one hinted module's operation state until the durable operation reaches a terminal state.
    /// </summary>
    /// <remarks>
    /// Timeout behavior matches the operation-id overload: timeout throws
    /// <see cref="TimeoutException"/> and does not finalize the operation.
    /// The module hint limits polling to one operation-state store.
    /// </remarks>
    /// <typeparam name="TResult">The expected result payload type.</typeparam>
    /// <param name="durableOperationId">The durable operation identifier returned or supplied when the command was sent.</param>
    /// <param name="moduleName">The module whose operation-state store should be queried.</param>
    /// <param name="timeout">The maximum time to wait for a terminal operation state.</param>
    /// <param name="pollInterval">An optional polling interval. When omitted, the reader uses its default interval.</param>
    /// <param name="ct">A cancellation token for the wait operation.</param>
    /// <returns>The terminal durable operation result state.</returns>
    /// <exception cref="TimeoutException">Thrown when the operation does not reach a terminal state before <paramref name="timeout"/> expires.</exception>
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

    /// <summary>
    /// Polls one hinted module's operation state until the durable operation reaches a terminal state or the caller's timeout expires.
    /// </summary>
    /// <remarks>
    /// Timeout behavior matches the operation-id overload: timeout returns
    /// <see cref="DurableOperationWaitResult{TResult}.CompletedWithinTimeout"/>
    /// as <see langword="false"/> and does not finalize the operation. The
    /// module hint limits polling to one operation-state store.
    /// </remarks>
    /// <typeparam name="TResult">The expected result payload type.</typeparam>
    /// <param name="durableOperationId">The durable operation identifier returned or supplied when the command was sent.</param>
    /// <param name="moduleName">The module whose operation-state store should be queried.</param>
    /// <param name="timeout">The maximum time to wait for a terminal operation state.</param>
    /// <param name="pollInterval">An optional polling interval. When omitted, the reader uses its default interval.</param>
    /// <param name="ct">A cancellation token for the wait operation.</param>
    /// <returns>The caller wait outcome and latest durable operation result state.</returns>
    ValueTask<DurableOperationWaitResult<TResult>> TryWaitForResultAsync<TResult>(
        Guid durableOperationId,
        string moduleName,
        TimeSpan timeout,
        TimeSpan? pollInterval = null,
        CancellationToken ct = default)
    {
        return TryWaitForResultAsync<TResult>(
            durableOperationId,
            timeout,
            pollInterval,
            ct);
    }

    /// <summary>
    /// Polls the target module named by a durable operation handle until the operation reaches a terminal state.
    /// </summary>
    /// <remarks>
    /// Prefer this overload when a durable send returned an operation handle.
    /// It waits against the target module's operation-state store and keeps
    /// timeout semantics separate from durable operation finalization.
    /// </remarks>
    /// <typeparam name="TResult">The expected result payload type.</typeparam>
    /// <param name="operation">The durable operation handle returned when the command was sent.</param>
    /// <param name="timeout">The maximum time to wait for a terminal operation state.</param>
    /// <param name="pollInterval">An optional polling interval. When omitted, the reader uses its default interval.</param>
    /// <param name="ct">A cancellation token for the wait operation.</param>
    /// <returns>The terminal durable operation result state.</returns>
    /// <exception cref="TimeoutException">Thrown when the operation does not reach a terminal state before <paramref name="timeout"/> expires.</exception>
    ValueTask<DurableOperationResult<TResult>> WaitForResultAsync<TResult>(
        DurableOperationHandle operation,
        TimeSpan timeout,
        TimeSpan? pollInterval = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return WaitForResultAsync<TResult>(
            operation.DurableOperationId,
            operation.TargetModule,
            timeout,
            pollInterval,
            ct);
    }

    /// <summary>
    /// Polls the target module named by a durable operation handle until the operation reaches a terminal state or the caller's timeout expires.
    /// </summary>
    /// <remarks>
    /// Prefer this overload when a durable send returned an operation handle.
    /// It waits against the target module's operation-state store and reports
    /// caller timeout separately from durable operation state.
    /// </remarks>
    /// <typeparam name="TResult">The expected result payload type.</typeparam>
    /// <param name="operation">The durable operation handle returned when the command was sent.</param>
    /// <param name="timeout">The maximum time to wait for a terminal operation state.</param>
    /// <param name="pollInterval">An optional polling interval. When omitted, the reader uses its default interval.</param>
    /// <param name="ct">A cancellation token for the wait operation.</param>
    /// <returns>The caller wait outcome and latest durable operation result state.</returns>
    ValueTask<DurableOperationWaitResult<TResult>> TryWaitForResultAsync<TResult>(
        DurableOperationHandle operation,
        TimeSpan timeout,
        TimeSpan? pollInterval = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return TryWaitForResultAsync<TResult>(
            operation.DurableOperationId,
            operation.TargetModule,
            timeout,
            pollInterval,
            ct);
    }

    private async ValueTask<DurableOperationWaitResult<TResult>> TryWaitForResultCoreAsync<TResult>(
        Guid durableOperationId,
        TimeSpan timeout,
        TimeSpan? pollInterval,
        CancellationToken ct)
    {
        try
        {
            DurableOperationResult<TResult> result =
                await WaitForResultAsync<TResult>(
                    durableOperationId,
                    timeout,
                    pollInterval,
                    ct);

            return new DurableOperationWaitResult<TResult>(
                completedWithinTimeout: true,
                result);
        }
        catch (TimeoutException)
        {
            DurableOperationResult<TResult> result =
                await GetResultAsync<TResult>(
                    durableOperationId,
                    ct);

            return new DurableOperationWaitResult<TResult>(
                completedWithinTimeout: false,
                result);
        }
    }
}
