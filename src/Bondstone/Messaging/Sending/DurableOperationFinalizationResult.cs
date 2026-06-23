namespace Bondstone.Messaging;

/// <summary>
/// Describes the result of an explicit durable operation finalization attempt.
/// </summary>
public sealed record DurableOperationFinalizationResult
{
    /// <summary>
    /// Initializes a new finalization result.
    /// </summary>
    /// <param name="state">The operation state returned or written by the finalization attempt.</param>
    /// <param name="wasFinalized">Whether the finalization attempt wrote a new terminal state.</param>
    public DurableOperationFinalizationResult(
        DurableOperationState state,
        bool wasFinalized)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
        WasFinalized = wasFinalized;
    }

    /// <summary>
    /// Gets the operation state returned or written by the finalization attempt.
    /// </summary>
    public DurableOperationState State { get; }

    /// <summary>
    /// Gets whether the finalization attempt wrote a new terminal state.
    /// </summary>
    public bool WasFinalized { get; }

    /// <summary>
    /// Gets the durable operation id.
    /// </summary>
    public Guid DurableOperationId => State.DurableOperationId;

    /// <summary>
    /// Gets the resulting operation status.
    /// </summary>
    public DurableOperationStatus Status => State.Status;

    /// <summary>
    /// Gets whether the resulting operation status is terminal.
    /// </summary>
    public bool IsTerminal =>
        Status is DurableOperationStatus.Completed
            or DurableOperationStatus.Failed
            or DurableOperationStatus.Cancelled;
}
