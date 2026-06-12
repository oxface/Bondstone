using Bondstone.Utility;
using System.ComponentModel;

namespace Bondstone.Persistence;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DurableModuleInboxHandlerExecutorRegistration
{
    private readonly Func<IServiceProvider, IDurableInboxHandlerExecutor> _createExecutor;

    /// <remarks>
    /// The factory runs inside the current DI scope for the selected module.
    /// It should return a lightweight wrapper over DI-owned scoped services and
    /// should not create owned disposable resources outside DI ownership.
    /// </remarks>
    public DurableModuleInboxHandlerExecutorRegistration(
        string moduleName,
        Func<IServiceProvider, IDurableInboxHandlerExecutor> createExecutor)
    {
        ModuleName = moduleName.NormalizeRequired(nameof(moduleName), "Module name");
        _createExecutor = createExecutor ?? throw new ArgumentNullException(nameof(createExecutor));
    }

    public string ModuleName { get; }

    public IDurableInboxHandlerExecutor CreateExecutor(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return _createExecutor(serviceProvider)
            ?? throw new InvalidOperationException(
                $"Durable module inbox handler executor factory for module '{ModuleName}' returned null.");
    }
}
