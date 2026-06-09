using Bondstone.Persistence;
using Bondstone.Persistence.Dapper.Postgres.Persistence;
using Bondstone.Utility;
using Npgsql;

namespace Bondstone.Persistence.Dapper.Postgres.Outbox;

public sealed class PostgresDapperModuleDurableOutboxDispatcher(
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
            new PostgresDapperDurableOutboxClaimer(
                dataSource,
                _timeProvider,
                schema),
            new PostgresDapperDurableOutboxLeaseRenewer(
                dataSource,
                _timeProvider,
                schema),
            transport,
            failurePolicy,
            new PostgresDapperDurableOutboxDispatchRecorder(
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
