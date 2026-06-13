using Bondstone.Modules;
using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Bondstone.Persistence.Postgres.Persistence;

internal sealed class PostgresModuleTransactionRunner(
    IServiceProvider serviceProvider,
    PostgresModuleRuntimeRegistry moduleRuntimeRegistry)
{
    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly PostgresModuleRuntimeRegistry _moduleRuntimeRegistry =
        moduleRuntimeRegistry ?? throw new ArgumentNullException(nameof(moduleRuntimeRegistry));

    public async ValueTask ExecuteAsync(
        IModulePipelineExecutionContext context,
        Func<CancellationToken, ValueTask> next,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        PostgresModuleRuntimeDescriptor runtime =
            _moduleRuntimeRegistry.GetRuntime(context.ModuleName);
        if (!runtime.UsesPostgresPersistence)
        {
            await next(ct);
            return;
        }

        IPostgresModuleSession session = _serviceProvider
            .GetRequiredService<IPostgresModuleSession>();
        PostgresModuleTransactionGuard transactionGuard = _serviceProvider
            .GetRequiredService<PostgresModuleTransactionGuard>();

        using IDisposable transactionScope = transactionGuard.Enter(context.ModuleName);

        bool transactionAlreadyActive = session.Transaction is not null;
        var transactionFeature = new PostgresModuleTransactionFeature(
            observesCommit: !transactionAlreadyActive);
        using IDisposable transactionFeatureScope = context.Features.Push<IModuleTransactionFeature>(
            transactionFeature);

        try
        {
            await session.ExecuteInTransactionAsync(next, ct);
        }
        catch
        {
            if (!transactionAlreadyActive)
            {
                await transactionFeature.RolledBackAsync(ct);
            }

            throw;
        }

        if (!transactionAlreadyActive)
        {
            await transactionFeature.CommittedAsync(ct);
        }
    }
}
