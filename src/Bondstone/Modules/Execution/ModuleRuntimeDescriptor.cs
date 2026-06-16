using Bondstone.Persistence;

namespace Bondstone.Modules;

internal sealed class ModuleRuntimeDescriptor(
    BondstoneModuleRegistration module,
    Lazy<IDurableOutboxWriter?> durableOutboxWriter,
    Lazy<IDurableInboxHandlerExecutor?> durableInboxHandlerExecutor,
    Lazy<IDurableInboxInspectionStore?> durableInboxInspectionStore,
    Lazy<IDurableOperationStateStore?> durableOperationStateStore,
    Lazy<IDurableOutboxInspectionStore?> durableOutboxInspectionStore)
{
    private readonly Lazy<IDurableOutboxWriter?> _durableOutboxWriter =
        durableOutboxWriter ?? throw new ArgumentNullException(nameof(durableOutboxWriter));
    private readonly Lazy<IDurableInboxHandlerExecutor?>
        _durableInboxHandlerExecutor = durableInboxHandlerExecutor
            ?? throw new ArgumentNullException(nameof(durableInboxHandlerExecutor));
    private readonly Lazy<IDurableInboxInspectionStore?> _durableInboxInspectionStore =
        durableInboxInspectionStore
        ?? throw new ArgumentNullException(nameof(durableInboxInspectionStore));
    private readonly Lazy<IDurableOperationStateStore?> _durableOperationStateStore =
        durableOperationStateStore
        ?? throw new ArgumentNullException(nameof(durableOperationStateStore));
    private readonly Lazy<IDurableOutboxInspectionStore?> _durableOutboxInspectionStore =
        durableOutboxInspectionStore
        ?? throw new ArgumentNullException(nameof(durableOutboxInspectionStore));

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

    public bool TryGetDurableInboxInspectionStore(
        out IDurableInboxInspectionStore? store)
    {
        store = _durableInboxInspectionStore.Value;
        return store is not null;
    }

    public bool TryGetDurableOperationStateStore(
        out IDurableOperationStateStore? store)
    {
        store = _durableOperationStateStore.Value;
        return store is not null;
    }

    public bool TryGetDurableOutboxInspectionStore(
        out IDurableOutboxInspectionStore? store)
    {
        store = _durableOutboxInspectionStore.Value;
        return store is not null;
    }
}
