using Bondstone.Persistence;
using Bondstone.Persistence.Postgres.Persistence;
using Bondstone.Utility;

namespace Bondstone.Persistence.Postgres.Inbox;

public sealed class PostgresModuleDurableInboxHandlerExecutor(
    string moduleName,
    IPostgresModuleSession session,
    TimeProvider? timeProvider = null,
    string? schema = null)
    : IDurableInboxHandlerExecutor
{
    private readonly DurableInboxHandlerExecutor _executor = new(
        new PostgresDurableInboxRegistrar(session, schema),
        new PostgresDurableInboxStore(session, schema),
        timeProvider);

    public string ModuleName { get; } = moduleName.NormalizeRequired(
        nameof(moduleName),
        "Module name");

    public ValueTask<DurableInboxHandleResult> HandleOnceAsync(
        DurableInboxRecord record,
        Func<CancellationToken, ValueTask> handler,
        CancellationToken ct = default)
    {
        return _executor.HandleOnceAsync(record, handler, ct);
    }
}
