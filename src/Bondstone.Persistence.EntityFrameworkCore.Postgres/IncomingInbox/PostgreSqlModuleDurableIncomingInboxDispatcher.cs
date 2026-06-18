using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Utility;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.IncomingInbox;

internal sealed class PostgreSqlModuleDurableIncomingInboxDispatcher<TDbContext>
    : IDurableIncomingInboxDispatcher
    where TDbContext : DbContext
{
    private readonly DurableIncomingInboxDispatcher _dispatcher;

    public PostgreSqlModuleDurableIncomingInboxDispatcher(
        string moduleName,
        TDbContext context,
        IModuleCommandReceivePipeline commandReceivePipeline,
        IModuleEventReceivePipeline eventReceivePipeline,
        IDurableIncomingInboxFailurePolicy failurePolicy,
        TimeProvider? timeProvider = null,
        string? schema = null)
    {
        TimeProvider resolvedTimeProvider = timeProvider ?? TimeProvider.System;
        ModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");
        _dispatcher = new DurableIncomingInboxDispatcher(
            new PostgreSqlDurableIncomingInboxClaimer<TDbContext>(
                context,
                resolvedTimeProvider,
                schema,
                receiverModuleName: ModuleName),
            commandReceivePipeline,
            eventReceivePipeline,
            new PostgreSqlDurableIncomingInboxOutcomeRecorder<TDbContext>(
                context,
                schema),
            failurePolicy,
            resolvedTimeProvider);
    }

    public string ModuleName { get; }

    public ValueTask<DurableIncomingInboxProcessingResult> ProcessAsync(
        string claimedBy,
        TimeSpan leaseDuration,
        int maxCount = 100,
        CancellationToken ct = default)
    {
        return _dispatcher.ProcessAsync(
            claimedBy,
            leaseDuration,
            maxCount,
            ct);
    }
}
