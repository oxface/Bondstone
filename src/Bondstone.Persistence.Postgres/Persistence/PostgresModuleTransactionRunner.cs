using Bondstone.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Bondstone.Persistence.Postgres.Persistence;

internal sealed class PostgresModuleTransactionRunner(
    IServiceProvider serviceProvider,
    IBondstoneModuleRegistry moduleRegistry)
{
    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly IBondstoneModuleRegistry _moduleRegistry =
        moduleRegistry ?? throw new ArgumentNullException(nameof(moduleRegistry));

    public async ValueTask ExecuteAsync(
        string moduleName,
        Func<CancellationToken, ValueTask> next,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(next);

        BondstoneModuleRegistration module = _moduleRegistry.GetModule(moduleName);
        if (!module.UsesPersistence
            || !StringComparer.Ordinal.Equals(
                module.PersistenceProviderName,
                PostgresModulePersistence.ProviderName))
        {
            await next(ct);
            return;
        }

        IPostgresModuleSession session = _serviceProvider
            .GetRequiredService<IPostgresModuleSession>();

        await session.ExecuteInTransactionAsync(next, ct);
    }
}
