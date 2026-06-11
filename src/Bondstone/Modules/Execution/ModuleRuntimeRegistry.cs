using Bondstone.Persistence;
using Bondstone.Utility;

namespace Bondstone.Modules;

internal sealed class ModuleRuntimeRegistry
{
    private readonly IBondstoneModuleRegistry _moduleRegistry;
    private readonly Lazy<IReadOnlyDictionary<string, IDurableModuleOutboxWriter>>
        _outboxWriters;
    private readonly Lazy<IReadOnlyDictionary<string, IDurableModuleInboxHandlerExecutor>>
        _inboxHandlerExecutors;
    private readonly Lazy<IReadOnlyDictionary<string, IDurableModuleOperationStateStore>>
        _operationStateStores;

    public ModuleRuntimeRegistry(
        IBondstoneModuleRegistry moduleRegistry,
        IEnumerable<IDurableModuleOutboxWriter> outboxWriters,
        IEnumerable<IDurableModuleInboxHandlerExecutor> inboxHandlerExecutors,
        IEnumerable<IDurableModuleOperationStateStore> operationStateStores)
    {
        _moduleRegistry = moduleRegistry ?? throw new ArgumentNullException(nameof(moduleRegistry));
        ArgumentNullException.ThrowIfNull(outboxWriters);
        ArgumentNullException.ThrowIfNull(inboxHandlerExecutors);
        ArgumentNullException.ThrowIfNull(operationStateStores);

        _outboxWriters = new Lazy<IReadOnlyDictionary<string, IDurableModuleOutboxWriter>>(
            () => ToModuleMap(
                outboxWriters,
                static writer => writer.ModuleName,
                "durable module outbox writer"));
        _inboxHandlerExecutors =
            new Lazy<IReadOnlyDictionary<string, IDurableModuleInboxHandlerExecutor>>(
                () => ToModuleMap(
                    inboxHandlerExecutors,
                    static executor => executor.ModuleName,
                    "durable module inbox handler executor"));
        _operationStateStores =
            new Lazy<IReadOnlyDictionary<string, IDurableModuleOperationStateStore>>(
                () => ToModuleMap(
                    operationStateStores,
                    static store => store.ModuleName,
                    "durable module operation-state store"));
    }

    public bool HasDurableOutboxWriters => _outboxWriters.Value.Count > 0;

    public bool HasDurableInboxHandlerExecutors =>
        _inboxHandlerExecutors.Value.Count > 0;

    public bool HasDurableOperationStateStores =>
        _operationStateStores.Value.Count > 0;

    public IReadOnlyCollection<IDurableModuleOperationStateStore>
        DurableOperationStateStores => _operationStateStores.Value.Values.ToArray();

    public void ValidateDurableOutboxWriters()
    {
        _ = _outboxWriters.Value;
    }

    public void ValidateDurableInboxHandlerExecutors()
    {
        _ = _inboxHandlerExecutors.Value;
    }

    public void ValidateDurableOperationStateStores()
    {
        _ = _operationStateStores.Value;
    }

    public bool TryGetRuntime(
        string moduleName,
        out ModuleRuntimeDescriptor? runtime)
    {
        if (_moduleRegistry.TryGetModule(moduleName, out BondstoneModuleRegistration? module)
            && module is not null)
        {
            runtime = CreateDescriptor(module);
            return true;
        }

        runtime = null;
        return false;
    }

    private ModuleRuntimeDescriptor CreateDescriptor(BondstoneModuleRegistration module)
    {
        return new ModuleRuntimeDescriptor(
            module,
            new Lazy<IDurableModuleOutboxWriter?>(
                () => GetModuleRegistration(_outboxWriters.Value, module.Name)),
            new Lazy<IDurableModuleInboxHandlerExecutor?>(
                () => GetModuleRegistration(_inboxHandlerExecutors.Value, module.Name)),
            new Lazy<IDurableModuleOperationStateStore?>(
                () => GetModuleRegistration(_operationStateStores.Value, module.Name)));
    }

    private static TRegistration? GetModuleRegistration<TRegistration>(
        IReadOnlyDictionary<string, TRegistration> registrationsByModule,
        string moduleName)
        where TRegistration : class
    {
        if (registrationsByModule.TryGetValue(
            NormalizeModuleName(moduleName),
            out TRegistration? registration))
        {
            return registration;
        }

        return null;
    }

    private static IReadOnlyDictionary<string, TRegistration> ToModuleMap<TRegistration>(
        IEnumerable<TRegistration> registrations,
        Func<TRegistration, string> moduleNameSelector,
        string registrationDescription)
    {
        TRegistration[] validatedRegistrations =
            DurableModulePersistenceRegistrationValidator.ToValidatedArray(
                registrations,
                moduleNameSelector,
                registrationDescription);

        return validatedRegistrations.ToDictionary(
            registration => NormalizeModuleName(moduleNameSelector(registration)),
            StringComparer.Ordinal);
    }

    private static string NormalizeModuleName(string moduleName)
    {
        return moduleName.NormalizeRequired(nameof(moduleName), "Module name");
    }
}
