namespace Bondstone.Persistence;

public sealed class DurableInboxHandlerExecutor(
    IDurableInboxRegistrar registrar,
    IDurableInboxStore inboxStore,
    TimeProvider? timeProvider = null)
    : IDurableInboxHandlerExecutor
{
    private readonly IDurableInboxRegistrar _registrar =
        registrar ?? throw new ArgumentNullException(nameof(registrar));
    private readonly IDurableInboxStore _inboxStore =
        inboxStore ?? throw new ArgumentNullException(nameof(inboxStore));
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async ValueTask<DurableInboxHandleResult> HandleOnceAsync(
        DurableInboxRecord record,
        Func<CancellationToken, ValueTask> handler,
        Func<CancellationToken, ValueTask> commit,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(commit);

        DurableInboxRegistrationResult registration = await _registrar.RegisterAsync(
            record,
            ct);

        if (registration.Status == DurableInboxRegistrationStatus.AlreadyProcessed)
        {
            return new DurableInboxHandleResult(
                DurableInboxHandleStatus.AlreadyProcessed,
                registration.Record);
        }

        if (registration.Status == DurableInboxRegistrationStatus.AlreadyReceived)
        {
            return new DurableInboxHandleResult(
                DurableInboxHandleStatus.AlreadyReceived,
                registration.Record);
        }

        await handler(ct);

        DateTimeOffset processedAtUtc = _timeProvider.GetUtcNow();
        await _inboxStore.MarkProcessedAsync(
            registration.Record.Key,
            processedAtUtc,
            ct);

        await commit(ct);

        return new DurableInboxHandleResult(
            DurableInboxHandleStatus.Handled,
            registration.Record.MarkProcessed(processedAtUtc));
    }
}
