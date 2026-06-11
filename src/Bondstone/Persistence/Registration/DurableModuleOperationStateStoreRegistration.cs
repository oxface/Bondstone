using Bondstone.Utility;

namespace Bondstone.Persistence;

public sealed class DurableModuleOperationStateStoreRegistration
{
    private readonly Func<IServiceProvider, IDurableOperationStateStore> _createStore;

    public DurableModuleOperationStateStoreRegistration(
        string moduleName,
        Func<IServiceProvider, IDurableOperationStateStore> createStore)
    {
        ModuleName = moduleName.NormalizeRequired(nameof(moduleName), "Module name");
        _createStore = createStore ?? throw new ArgumentNullException(nameof(createStore));
    }

    public string ModuleName { get; }

    internal IDurableOperationStateStore CreateStore(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return _createStore(serviceProvider)
            ?? throw new InvalidOperationException(
                $"Durable module operation-state store factory for module '{ModuleName}' returned null.");
    }
}
