using Bondstone.Utility;
using System.ComponentModel;

namespace Bondstone.Persistence;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DurableModuleInboxInspectionStoreRegistration
{
    private readonly Func<IServiceProvider, IDurableInboxInspectionStore> _createStore;

    /// <remarks>
    /// The factory runs inside the current DI scope for the selected module.
    /// It should return a lightweight wrapper over DI-owned scoped services and
    /// should not create owned disposable resources outside DI ownership.
    /// </remarks>
    public DurableModuleInboxInspectionStoreRegistration(
        string moduleName,
        Func<IServiceProvider, IDurableInboxInspectionStore> createStore)
    {
        ModuleName = moduleName.NormalizeRequired(nameof(moduleName), "Module name");
        _createStore = createStore ?? throw new ArgumentNullException(nameof(createStore));
    }

    public string ModuleName { get; }

    public IDurableInboxInspectionStore CreateStore(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return _createStore(serviceProvider)
            ?? throw new InvalidOperationException(
                $"Durable module inbox inspection store factory for module '{ModuleName}' returned null.");
    }
}
