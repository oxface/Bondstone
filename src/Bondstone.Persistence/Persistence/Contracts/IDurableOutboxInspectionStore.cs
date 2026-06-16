namespace Bondstone.Persistence;

public interface IDurableOutboxInspectionStore
{
    ValueTask<IReadOnlyList<DurableOutboxRecord>> FindTerminalFailedAsync(
        int maxCount = 100,
        DateTimeOffset? failedAtOrBeforeUtc = null,
        string? sourceModuleName = null,
        CancellationToken ct = default);
}
