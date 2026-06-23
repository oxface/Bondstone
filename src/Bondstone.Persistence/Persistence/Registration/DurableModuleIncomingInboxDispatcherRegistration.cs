using Bondstone.Utility;
using System.ComponentModel;

namespace Bondstone.Persistence;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DurableModuleIncomingInboxDispatcherRegistration
{
    private readonly Func<IServiceProvider, IDurableIncomingInboxDispatcher> _createDispatcher;

    /// <remarks>
    /// The factory runs inside the current DI scope for the selected module.
    /// It should return a lightweight wrapper over DI-owned scoped services and
    /// should not create owned disposable resources outside DI ownership.
    /// </remarks>
    public DurableModuleIncomingInboxDispatcherRegistration(
        string moduleName,
        Func<IServiceProvider, IDurableIncomingInboxDispatcher> createDispatcher)
    {
        ModuleName = moduleName.NormalizeRequired(nameof(moduleName), "Module name");
        _createDispatcher = createDispatcher ?? throw new ArgumentNullException(nameof(createDispatcher));
    }

    public string ModuleName { get; }

    public IDurableIncomingInboxDispatcher CreateDispatcher(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return _createDispatcher(serviceProvider)
            ?? throw new InvalidOperationException(
                $"Durable module incoming inbox dispatcher factory for module '{ModuleName}' returned null.");
    }
}
