using Bondstone.Utility;

namespace Bondstone.Modules;

public sealed class ModuleExecutionContext
{
    public ModuleExecutionContext(string moduleName)
    {
        ModuleName = moduleName.NormalizeRequired(nameof(moduleName), "Module name");
    }

    public string ModuleName { get; }
}

