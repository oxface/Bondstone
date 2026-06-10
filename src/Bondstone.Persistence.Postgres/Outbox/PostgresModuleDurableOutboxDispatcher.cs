using Bondstone.Persistence;
using Bondstone.Persistence.Postgres.Persistence;
using Bondstone.Utility;
using Npgsql;

namespace Bondstone.Persistence.Postgres.Outbox;

public sealed class PostgresModuleDurableOutboxDispatcher(
    string moduleName,
    NpgsqlDataSource dataSource,
    IDurableOutboxTransport transport,
    IDurableOutboxFailurePolicy failurePolicy,
    TimeProvider? timeProvider = null,
    string? schema = null)
    : IDurableModuleOutboxDispatcher
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
            new PostgresDurableOutboxClaimer(
                dataSource,
                _timeProvider,
                schema),
            new PostgresDurableOutboxLeaseRenewer(
                dataSource,
                _timeProvider,
                schema),
            transport,
            failurePolicy,
            new PostgresDurableOutboxDispatchRecorder(
                dataSource,
                schema),
            _timeProvider);

        return await dispatcher.DispatchAsync(
            claimedBy,
            leaseDuration,
            maxCount,
            ct);
    }
}
