namespace Bondstone.Persistence;

public interface IDurableModuleInboxHandlerExecutor : IDurableInboxHandlerExecutor
{
    string ModuleName { get; }
}
