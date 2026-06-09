namespace Bondstone.Modules;

public interface IModuleExecutionContextAccessor
{
    ModuleExecutionContext? Current { get; }
}

