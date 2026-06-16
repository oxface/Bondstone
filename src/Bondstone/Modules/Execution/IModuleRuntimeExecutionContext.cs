namespace Bondstone.Modules;

public interface IModuleRuntimeExecutionContext
{
    string ModuleName { get; }

    ModuleRuntimeFeatureCollection Features { get; }
}
