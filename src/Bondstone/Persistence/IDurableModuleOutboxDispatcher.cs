namespace Bondstone.Persistence;

public interface IDurableModuleOutboxDispatcher : IDurableOutboxDispatcher
{
    string ModuleName { get; }
}
