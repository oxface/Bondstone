namespace Bondstone.Persistence;

public interface IDurableModuleOutboxWriter : IDurableOutboxWriter
{
    string ModuleName { get; }
}
