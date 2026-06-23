namespace Bondstone.Messaging;

/// <summary>
/// Represents the outcome of waiting for a durable operation result.
/// </summary>
/// <typeparam name="TResult">The expected result payload type.</typeparam>
/// <remarks>
/// This type separates caller wait outcome from durable operation state.
/// Timeout is caller patience and does not mean the operation is failed,
/// cancelled, or unknown.
/// </remarks>
public sealed record DurableOperationWaitResult<TResult>
{
    public DurableOperationWaitResult(
        bool completedWithinTimeout,
        DurableOperationResult<TResult> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (completedWithinTimeout && !result.IsTerminal)
        {
            throw new ArgumentException(
                "A wait result that completed within timeout must carry a terminal durable operation result.",
                nameof(result));
        }

        CompletedWithinTimeout = completedWithinTimeout;
        Result = result;
    }

    /// <summary>
    /// Gets whether a terminal operation result was observed before the caller's timeout expired.
    /// </summary>
    public bool CompletedWithinTimeout { get; }

    /// <summary>
    /// Gets the latest observed durable operation result.
    /// </summary>
    public DurableOperationResult<TResult> Result { get; }
}
