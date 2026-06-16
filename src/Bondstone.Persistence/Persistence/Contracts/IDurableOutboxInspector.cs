namespace Bondstone.Persistence;

public interface IDurableOutboxInspector
{
    ValueTask<IReadOnlyList<DurableOutboxRecord>> FindTerminalFailedAsync(
        string moduleName,
        int maxCount = 100,
        DateTimeOffset? failedAtOrBeforeUtc = null,
        CancellationToken ct = default);
}
