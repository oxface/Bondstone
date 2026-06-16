namespace Bondstone.Persistence;

public interface IDurableInboxInspector
{
    ValueTask<IReadOnlyList<DurableInboxRecord>> FindUnprocessedAsync(
        string moduleName,
        int maxCount = 100,
        DateTimeOffset? receivedAtOrBeforeUtc = null,
        CancellationToken ct = default);
}
