using Bondstone.Utility;
using System.ComponentModel;

namespace Bondstone.Persistence;

/// <summary>
/// Registers the provider-side inbox inspection store for one module.
/// </summary>
/// <remarks>
/// This type is public for provider and advanced composition packages, but is
/// hidden from normal IntelliSense. Application setup should prefer the module
/// persistence extension methods that create these registrations.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DurableModuleInboxInspectionStoreRegistration
{
    private readonly Func<IServiceProvider, IDurableInboxInspectionStore> _createStore;

    /// <summary>
    /// Initializes the registration for one module inbox inspection store.
    /// </summary>
    /// <param name="moduleName">The module that owns the inspection store.</param>
    /// <param name="createStore">The scoped factory that resolves the provider inspection store.</param>
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

    /// <summary>
    /// Gets the module that owns the inspection store.
    /// </summary>
    public string ModuleName { get; }

    /// <summary>
    /// Creates the inspection store from the current DI scope.
    /// </summary>
    /// <param name="serviceProvider">The current scoped service provider.</param>
    /// <returns>The provider-side inbox inspection store.</returns>
    public IDurableInboxInspectionStore CreateStore(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return _createStore(serviceProvider)
            ?? throw new InvalidOperationException(
                $"Durable module inbox inspection store factory for module '{ModuleName}' returned null.");
    }
}
