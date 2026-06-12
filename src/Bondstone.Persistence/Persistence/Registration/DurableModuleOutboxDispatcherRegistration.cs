using Bondstone.Utility;

namespace Bondstone.Persistence;

public sealed class DurableModuleOutboxDispatcherRegistration
{
    private readonly Func<IServiceProvider, IDurableOutboxDispatcher> _createDispatcher;

    public DurableModuleOutboxDispatcherRegistration(
        string moduleName,
        Func<IServiceProvider, IDurableOutboxDispatcher> createDispatcher)
    {
        ModuleName = moduleName.NormalizeRequired(nameof(moduleName), "Module name");
        _createDispatcher = createDispatcher ?? throw new ArgumentNullException(nameof(createDispatcher));
    }

    public string ModuleName { get; }

    public IDurableOutboxDispatcher CreateDispatcher(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return _createDispatcher(serviceProvider);
    }
}
