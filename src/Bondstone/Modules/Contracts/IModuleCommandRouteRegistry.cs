namespace Bondstone.Modules;

public interface IModuleCommandRouteRegistry
{
    IReadOnlyCollection<ModuleCommandRoute> Routes { get; }

    ModuleCommandRoute GetByCommandType(
        string moduleName,
        Type commandType);

    ModuleCommandRoute GetByMessageTypeName(
        string moduleName,
        string messageTypeName);

    bool TryGetByCommandType(
        string moduleName,
        Type commandType,
        out ModuleCommandRoute? route);

    bool TryGetByMessageTypeName(
        string moduleName,
        string messageTypeName,
        out ModuleCommandRoute? route);
}
