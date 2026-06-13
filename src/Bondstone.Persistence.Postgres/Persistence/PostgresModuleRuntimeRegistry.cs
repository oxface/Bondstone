using Bondstone.Modules;

namespace Bondstone.Persistence.Postgres.Persistence;

internal sealed class PostgresModuleRuntimeRegistry(IBondstoneModuleRegistry moduleRegistry)
{
    private readonly IBondstoneModuleRegistry _moduleRegistry =
        moduleRegistry ?? throw new ArgumentNullException(nameof(moduleRegistry));

    public PostgresModuleRuntimeDescriptor GetRuntime(string moduleName)
    {
        return new PostgresModuleRuntimeDescriptor(_moduleRegistry.GetModule(moduleName));
    }
}
