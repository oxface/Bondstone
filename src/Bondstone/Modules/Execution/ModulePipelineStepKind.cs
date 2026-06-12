using System.ComponentModel;

namespace Bondstone.Modules;

[EditorBrowsable(EditorBrowsableState.Never)]
public enum ModulePipelineStepKind
{
    System,
    Capability,
    Application,
}
