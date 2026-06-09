namespace Bondstone.Persistence;

public interface IDurableInboxHandlerExecutor
{
    ValueTask<DurableInboxHandleResult> HandleOnceAsync(
        DurableInboxRecord record,
        Func<CancellationToken, ValueTask> handler,
        Func<CancellationToken, ValueTask> commit,
        CancellationToken ct = default);
}
