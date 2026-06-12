using Bondstone.Utility;
using System.ComponentModel;

namespace Bondstone.Persistence;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DurableModuleOperationStateStoreRegistration
{
    private readonly Func<IServiceProvider, IDurableOperationStateStore> _createStore;

    /// <remarks>
    /// The factory runs inside the current DI scope for the selected module.
    /// It should return a lightweight wrapper over DI-owned scoped services and
    /// should not create owned disposable resources outside DI ownership.
    /// </remarks>
    public DurableModuleOperationStateStoreRegistration(
        string moduleName,
        Func<IServiceProvider, IDurableOperationStateStore> createStore)
    {
        ModuleName = moduleName.NormalizeRequired(nameof(moduleName), "Module name");
        _createStore = createStore ?? throw new ArgumentNullException(nameof(createStore));
    }

    public string ModuleName { get; }

    public IDurableOperationStateStore CreateStore(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return _createStore(serviceProvider)
            ?? throw new InvalidOperationException(
                $"Durable module operation-state store factory for module '{ModuleName}' returned null.");
    }
}
