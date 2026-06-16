namespace Bondstone.Persistence;

public interface IDurableInboxInspectionStore
{
    ValueTask<IReadOnlyList<DurableInboxRecord>> FindUnprocessedAsync(
        int maxCount = 100,
        DateTimeOffset? receivedAtOrBeforeUtc = null,
        string? moduleName = null,
        CancellationToken ct = default);
}
