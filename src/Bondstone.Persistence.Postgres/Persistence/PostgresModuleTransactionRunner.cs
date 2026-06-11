using Bondstone.Modules;
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
        string moduleName,
        Func<CancellationToken, ValueTask> next,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(next);

        PostgresModuleRuntimeDescriptor runtime =
            _moduleRuntimeRegistry.GetRuntime(moduleName);
        if (!runtime.UsesPostgresPersistence)
        {
            await next(ct);
            return;
        }

        IPostgresModuleSession session = _serviceProvider
            .GetRequiredService<IPostgresModuleSession>();

        await session.ExecuteInTransactionAsync(next, ct);
    }
}
