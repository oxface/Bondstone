using Bondstone.Modules;

namespace Bondstone.Persistence.EntityFrameworkCore.Persistence;

internal sealed class EntityFrameworkCoreModuleRuntimeRegistry(
    IBondstoneModuleRegistry moduleRegistry)
{
    private readonly IBondstoneModuleRegistry _moduleRegistry =
        moduleRegistry ?? throw new ArgumentNullException(nameof(moduleRegistry));

    public EntityFrameworkCoreModuleRuntimeDescriptor GetRuntime(string moduleName)
    {
        BondstoneModuleRegistration module = _moduleRegistry.GetModule(moduleName);
        return new EntityFrameworkCoreModuleRuntimeDescriptor(module);
    }
}
