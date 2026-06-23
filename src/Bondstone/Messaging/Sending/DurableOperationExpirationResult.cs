namespace Bondstone.Messaging;

/// <summary>
/// Describes the result of one durable operation expiry processing pass.
/// </summary>
public sealed record DurableOperationExpirationResult
{
    /// <summary>
    /// Initializes a durable operation expiry result.
    /// </summary>
    /// <param name="moduleName">The module whose operation-state store was processed.</param>
    /// <param name="expiresBeforeUtc">The UTC candidate cutoff used by the processing pass.</param>
    /// <param name="terminalStatus">The terminal status requested for expired operations.</param>
    /// <param name="finalizations">The per-operation finalization results.</param>
    public DurableOperationExpirationResult(
        string moduleName,
        DateTimeOffset expiresBeforeUtc,
        DurableOperationStatus terminalStatus,
        IReadOnlyList<DurableOperationFinalizationResult> finalizations)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
        {
            throw new ArgumentException("Module name is required.", nameof(moduleName));
        }

        if (expiresBeforeUtc == default)
        {
            throw new ArgumentException("Expiry cutoff must not be the default value.", nameof(expiresBeforeUtc));
        }

        if (expiresBeforeUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Expiry cutoff must use UTC offset.", nameof(expiresBeforeUtc));
        }

        if (terminalStatus is not DurableOperationStatus.Failed and not DurableOperationStatus.Cancelled)
        {
            throw new ArgumentOutOfRangeException(
                nameof(terminalStatus),
                terminalStatus,
                "Expiry terminal status must be Failed or Cancelled.");
        }

        ModuleName = moduleName.Trim();
        ExpiresBeforeUtc = expiresBeforeUtc;
        TerminalStatus = terminalStatus;
        Finalizations = finalizations?.ToArray()
            ?? throw new ArgumentNullException(nameof(finalizations));
    }

    /// <summary>
    /// Gets the module whose operation-state store was processed.
    /// </summary>
    public string ModuleName { get; }

    /// <summary>
    /// Gets the UTC candidate cutoff used by the processing pass.
    /// </summary>
    public DateTimeOffset ExpiresBeforeUtc { get; }

    /// <summary>
    /// Gets the terminal status requested for expired operations.
    /// </summary>
    public DurableOperationStatus TerminalStatus { get; }

    /// <summary>
    /// Gets the per-operation finalization results.
    /// </summary>
    public IReadOnlyList<DurableOperationFinalizationResult> Finalizations { get; }

    /// <summary>
    /// Gets the number of candidate operations returned by the persistence store.
    /// </summary>
    public int CandidateCount => Finalizations.Count;

    /// <summary>
    /// Gets the number of operations that were newly finalized.
    /// </summary>
    public int FinalizedCount => Finalizations.Count(static finalization => finalization.WasFinalized);
}
