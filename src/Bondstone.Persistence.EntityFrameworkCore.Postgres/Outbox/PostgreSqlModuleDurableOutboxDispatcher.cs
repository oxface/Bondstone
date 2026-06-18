using Bondstone.Persistence;
using Bondstone.Utility;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.Outbox;

internal sealed class PostgreSqlModuleDurableOutboxDispatcher<TDbContext>
    : IDurableOutboxDispatcher
    where TDbContext : DbContext
{
    private readonly DurableOutboxDispatcher _dispatcher;

    public PostgreSqlModuleDurableOutboxDispatcher(
        string moduleName,
        TDbContext context,
        IDurableEnvelopeDispatcher envelopeDispatcher,
        IDurableOutboxFailurePolicy failurePolicy,
        TimeProvider? timeProvider = null,
        string? schema = null)
    {
        TimeProvider resolvedTimeProvider = timeProvider ?? TimeProvider.System;
        ModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");
        _dispatcher = new DurableOutboxDispatcher(
            new PostgreSqlDurableOutboxClaimer<TDbContext>(
                context,
                resolvedTimeProvider,
                schema,
                sourceModuleName: ModuleName),
            new PostgreSqlDurableOutboxLeaseRenewer<TDbContext>(
                context,
                resolvedTimeProvider,
                schema),
            envelopeDispatcher,
            failurePolicy,
            new PostgreSqlDurableOutboxDispatchRecorder<TDbContext>(
                context,
                schema),
            resolvedTimeProvider);
    }

    public string ModuleName { get; }

    public ValueTask<DurableOutboxDispatchResult> DispatchAsync(
        string claimedBy,
        TimeSpan leaseDuration,
        int maxCount = 100,
        CancellationToken ct = default)
    {
        return _dispatcher.DispatchAsync(
            claimedBy,
            leaseDuration,
            maxCount,
            ct);
    }
}
