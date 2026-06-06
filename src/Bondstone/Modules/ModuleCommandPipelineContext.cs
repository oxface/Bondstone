namespace Bondstone.Modules;

public sealed class ModuleCommandPipelineContext(ModuleCommandRoute route)
{
    public ModuleCommandRoute Route { get; } =
        route ?? throw new ArgumentNullException(nameof(route));

    public string ModuleName => Route.ModuleName;

    public string? MessageTypeName => Route.MessageTypeName;

    public string? HandlerIdentity => Route.HandlerIdentity;
}
