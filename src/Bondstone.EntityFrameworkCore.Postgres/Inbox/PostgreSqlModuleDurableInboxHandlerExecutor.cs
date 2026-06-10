using Bondstone.EntityFrameworkCore.Inbox;
using Bondstone.Persistence;
using Bondstone.Utility;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.EntityFrameworkCore.Postgres.Inbox;

public sealed class PostgreSqlModuleDurableInboxHandlerExecutor<TDbContext>(
    string moduleName,
    TDbContext context,
    TimeProvider? timeProvider = null,
    string? schema = null)
    : IDurableModuleInboxHandlerExecutor
    where TDbContext : DbContext
{
    private readonly DurableInboxHandlerExecutor _executor = new(
        new PostgreSqlDurableInboxRegistrar<TDbContext>(context, schema),
        new EntityFrameworkCoreDurableInboxStore<TDbContext>(context),
        timeProvider);

    public string ModuleName { get; } = moduleName.NormalizeRequired(
        nameof(moduleName),
        "Module name");

    public async ValueTask<DurableInboxHandleResult> HandleOnceAsync(
        DurableInboxRecord record,
        Func<CancellationToken, ValueTask> handler,
        CancellationToken ct = default)
    {
        return await _executor.HandleOnceAsync(
            record,
            handler,
            ct);
    }
}
