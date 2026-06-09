using Bondstone.Persistence;
using Bondstone.Persistence.Dapper.Postgres.Persistence;
using Bondstone.Utility;

namespace Bondstone.Persistence.Dapper.Postgres.Inbox;

public sealed class PostgresDapperModuleDurableInboxHandlerExecutor(
    string moduleName,
    IPostgresDapperModuleSession session,
    TimeProvider? timeProvider = null,
    string? schema = null)
    : IDurableModuleInboxHandlerExecutor
{
    private readonly DurableInboxHandlerExecutor _executor = new(
        new PostgresDapperDurableInboxRegistrar(session, schema),
        new PostgresDapperDurableInboxStore(session, schema),
        timeProvider);

    public string ModuleName { get; } = moduleName.NormalizeRequired(
        nameof(moduleName),
        "Module name");

    public ValueTask<DurableInboxHandleResult> HandleOnceAsync(
        DurableInboxRecord record,
        Func<CancellationToken, ValueTask> handler,
        Func<CancellationToken, ValueTask> commit,
        CancellationToken ct = default)
    {
        return _executor.HandleOnceAsync(record, handler, commit, ct);
    }
}
