using Bondstone.Persistence;

namespace Bondstone.Modules;

internal sealed class ModuleRuntimeDescriptor(
    BondstoneModuleRegistration module,
    Lazy<IDurableModuleOutboxWriter?> durableOutboxWriter,
    Lazy<IDurableModuleInboxHandlerExecutor?> durableInboxHandlerExecutor,
    Lazy<IDurableModuleOperationStateStore?> durableOperationStateStore)
{
    private readonly Lazy<IDurableModuleOutboxWriter?> _durableOutboxWriter =
        durableOutboxWriter ?? throw new ArgumentNullException(nameof(durableOutboxWriter));
    private readonly Lazy<IDurableModuleInboxHandlerExecutor?>
        _durableInboxHandlerExecutor = durableInboxHandlerExecutor
            ?? throw new ArgumentNullException(nameof(durableInboxHandlerExecutor));
    private readonly Lazy<IDurableModuleOperationStateStore?> _durableOperationStateStore =
        durableOperationStateStore
        ?? throw new ArgumentNullException(nameof(durableOperationStateStore));

    public BondstoneModuleRegistration Module { get; } =
        module ?? throw new ArgumentNullException(nameof(module));

    public string ModuleName => Module.Name;

    public bool TryGetDurableOutboxWriter(out IDurableOutboxWriter? writer)
    {
        writer = _durableOutboxWriter.Value;
        return writer is not null;
    }

    public bool TryGetDurableInboxHandlerExecutor(
        out IDurableInboxHandlerExecutor? executor)
    {
        executor = _durableInboxHandlerExecutor.Value;
        return executor is not null;
    }

    public bool TryGetDurableOperationStateStore(
        out IDurableOperationStateStore? store)
    {
        store = _durableOperationStateStore.Value;
        return store is not null;
    }
}
