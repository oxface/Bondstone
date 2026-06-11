using Bondstone.Utility;

namespace Bondstone.Persistence;

public sealed class DurableModuleInboxHandlerExecutorRegistration
{
    private readonly Func<IServiceProvider, IDurableInboxHandlerExecutor> _createExecutor;

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
