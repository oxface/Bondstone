namespace Bondstone.Persistence;

public interface IDurableModuleOperationStateStore : IDurableOperationStateStore
{
    string ModuleName { get; }
}
