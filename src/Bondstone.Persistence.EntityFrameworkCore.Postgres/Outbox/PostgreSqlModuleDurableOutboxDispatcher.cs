using Bondstone.Persistence;
using Bondstone.Utility;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.Outbox;

public sealed class PostgreSqlModuleDurableOutboxDispatcher<TDbContext>(
    string moduleName,
    TDbContext context,
    IDurableOutboxTransport transport,
    IDurableOutboxFailurePolicy failurePolicy,
    TimeProvider? timeProvider = null,
    string? schema = null)
    : IDurableOutboxDispatcher
    where TDbContext : DbContext
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public string ModuleName { get; } = moduleName.NormalizeRequired(
        nameof(moduleName),
        "Module name");

    public async ValueTask<DurableOutboxDispatchResult> DispatchAsync(
        string claimedBy,
        TimeSpan leaseDuration,
        int maxCount = 100,
        CancellationToken ct = default)
    {
        var dispatcher = new DurableOutboxDispatcher(
            new PostgreSqlDurableOutboxClaimer<TDbContext>(
                context,
                _timeProvider,
                schema),
            new PostgreSqlDurableOutboxLeaseRenewer<TDbContext>(
                context,
                _timeProvider,
                schema),
            transport,
            failurePolicy,
            new PostgreSqlDurableOutboxDispatchRecorder<TDbContext>(
                context,
                schema),
            _timeProvider);

        return await dispatcher.DispatchAsync(
            claimedBy,
            leaseDuration,
            maxCount,
            ct);
    }
}
