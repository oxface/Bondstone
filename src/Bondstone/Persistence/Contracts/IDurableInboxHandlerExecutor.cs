namespace Bondstone.Persistence;

public interface IDurableInboxHandlerExecutor
{
    ValueTask<DurableInboxHandleResult> HandleOnceAsync(
        DurableInboxRecord record,
        Func<CancellationToken, ValueTask> handler,
        CancellationToken ct = default);
}
