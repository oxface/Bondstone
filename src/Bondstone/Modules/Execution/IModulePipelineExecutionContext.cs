namespace Bondstone.Modules;

public interface IModulePipelineExecutionContext
{
    string ModuleName { get; }

    ModulePipelineFeatureCollection Features { get; }
}
