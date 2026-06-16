using Bondstone.Persistence;
using Bondstone.Utility;

namespace Bondstone.Modules;

internal sealed class ModuleRuntimeRegistry
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBondstoneModuleRegistry _moduleRegistry;
    private readonly Lazy<IReadOnlyDictionary<string, DurableModuleOutboxWriterRegistration>>
        _outboxWriterRegistrations;
    private readonly Lazy<IReadOnlyDictionary<string, DurableModuleInboxHandlerExecutorRegistration>>
        _inboxHandlerExecutorRegistrations;
    private readonly Lazy<IReadOnlyDictionary<string, DurableModuleInboxInspectionStoreRegistration>>
        _inboxInspectionStoreRegistrations;
    private readonly Lazy<IReadOnlyDictionary<string, DurableModuleOperationStateStoreRegistration>>
        _operationStateStoreRegistrations;
    private readonly Lazy<IReadOnlyDictionary<string, DurableModuleOutboxInspectionStoreRegistration>>
        _outboxInspectionStoreRegistrations;

    public ModuleRuntimeRegistry(
        IServiceProvider serviceProvider,
        IBondstoneModuleRegistry moduleRegistry,
        DurableModulePersistenceRegistrationRegistry persistenceRegistrationRegistry)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _moduleRegistry = moduleRegistry ?? throw new ArgumentNullException(nameof(moduleRegistry));
        ArgumentNullException.ThrowIfNull(persistenceRegistrationRegistry);

        _outboxWriterRegistrations =
            new Lazy<IReadOnlyDictionary<string, DurableModuleOutboxWriterRegistration>>(
            () => ToModuleMap(
                persistenceRegistrationRegistry.OutboxWriterRegistrations,
                static registration => registration.ModuleName,
                "durable module outbox writer"));
        _inboxHandlerExecutorRegistrations =
            new Lazy<IReadOnlyDictionary<string, DurableModuleInboxHandlerExecutorRegistration>>(
                () => ToModuleMap(
                    persistenceRegistrationRegistry.InboxHandlerExecutorRegistrations,
                    static registration => registration.ModuleName,
                    "durable module inbox handler executor"));
        _inboxInspectionStoreRegistrations =
            new Lazy<IReadOnlyDictionary<string, DurableModuleInboxInspectionStoreRegistration>>(
                () => ToModuleMap(
                    persistenceRegistrationRegistry.InboxInspectionStoreRegistrations,
                    static registration => registration.ModuleName,
                    "durable module inbox inspection store"));
        _operationStateStoreRegistrations =
            new Lazy<IReadOnlyDictionary<string, DurableModuleOperationStateStoreRegistration>>(
                () => ToModuleMap(
                    persistenceRegistrationRegistry.OperationStateStoreRegistrations,
                    static registration => registration.ModuleName,
                    "durable module operation-state store"));
        _outboxInspectionStoreRegistrations =
            new Lazy<IReadOnlyDictionary<string, DurableModuleOutboxInspectionStoreRegistration>>(
                () => ToModuleMap(
                    persistenceRegistrationRegistry.OutboxInspectionStoreRegistrations,
                    static registration => registration.ModuleName,
                    "durable module outbox inspection store"));
    }

    public bool HasDurableOutboxWriters => _outboxWriterRegistrations.Value.Count > 0;

    public bool HasDurableInboxHandlerExecutors =>
        _inboxHandlerExecutorRegistrations.Value.Count > 0;

    public bool HasDurableInboxInspectionStores =>
        _inboxInspectionStoreRegistrations.Value.Count > 0;

    public bool HasDurableOperationStateStores =>
        _operationStateStoreRegistrations.Value.Count > 0;

    public bool HasDurableOutboxInspectionStores =>
        _outboxInspectionStoreRegistrations.Value.Count > 0;

    public bool HasDurableModulePersistenceRegistrations =>
        HasDurableOutboxWriters
        || HasDurableInboxHandlerExecutors
        || HasDurableInboxInspectionStores
        || HasDurableOperationStateStores
        || HasDurableOutboxInspectionStores;

    public void ValidateDurableOutboxWriters()
    {
        _ = _outboxWriterRegistrations.Value;
    }

    public void ValidateDurableInboxHandlerExecutors()
    {
        _ = _inboxHandlerExecutorRegistrations.Value;
    }

    public void ValidateDurableInboxInspectionStores()
    {
        _ = _inboxInspectionStoreRegistrations.Value;
    }

    public void ValidateDurableOperationStateStores()
    {
        _ = _operationStateStoreRegistrations.Value;
    }

    public void ValidateDurableOutboxInspectionStores()
    {
        _ = _outboxInspectionStoreRegistrations.Value;
    }

    public IReadOnlyList<IDurableOperationStateStore> CreateDurableOperationStateStores()
    {
        return _operationStateStoreRegistrations.Value.Values
            .Select(registration => registration.CreateStore(_serviceProvider))
            .ToArray();
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
            new Lazy<IDurableOutboxWriter?>(
                () => CreateModuleService(
                    _outboxWriterRegistrations.Value,
                    module.Name,
                    registration => registration.CreateWriter(_serviceProvider))),
            new Lazy<IDurableInboxHandlerExecutor?>(
                () => CreateModuleService(
                    _inboxHandlerExecutorRegistrations.Value,
                    module.Name,
                    registration => registration.CreateExecutor(_serviceProvider))),
            new Lazy<IDurableInboxInspectionStore?>(
                () => CreateModuleService(
                    _inboxInspectionStoreRegistrations.Value,
                    module.Name,
                    registration => registration.CreateStore(_serviceProvider))),
            new Lazy<IDurableOperationStateStore?>(
                () => CreateModuleService(
                    _operationStateStoreRegistrations.Value,
                    module.Name,
                    registration => registration.CreateStore(_serviceProvider))),
            new Lazy<IDurableOutboxInspectionStore?>(
                () => CreateModuleService(
                    _outboxInspectionStoreRegistrations.Value,
                    module.Name,
                    registration => registration.CreateStore(_serviceProvider))));
    }

    private static TService? CreateModuleService<TRegistration, TService>(
        IReadOnlyDictionary<string, TRegistration> registrationsByModule,
        string moduleName,
        Func<TRegistration, TService> createService)
        where TRegistration : class
        where TService : class
    {
        if (registrationsByModule.TryGetValue(
            NormalizeModuleName(moduleName),
            out TRegistration? registration))
        {
            return createService(registration);
        }

        return null;
    }

    private static IReadOnlyDictionary<string, TRegistration> ToModuleMap<TRegistration>(
        IEnumerable<TRegistration> registrations,
        Func<TRegistration, string> moduleNameSelector,
        string registrationDescription)
    {
        TRegistration[] validatedRegistrations = registrations.ToArray();
        string[] duplicateModuleNames = validatedRegistrations
            .Select(registration => NormalizeModuleName(moduleNameSelector(registration)))
            .GroupBy(static moduleName => moduleName, StringComparer.Ordinal)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .OrderBy(static moduleName => moduleName, StringComparer.Ordinal)
            .ToArray();

        if (duplicateModuleNames.Length > 0)
        {
            throw new InvalidOperationException(
                $"Multiple {registrationDescription} registrations exist for module(s): '{string.Join("', '", duplicateModuleNames)}'. Configure exactly one {registrationDescription} per module.");
        }

        return validatedRegistrations.ToDictionary(
            registration => NormalizeModuleName(moduleNameSelector(registration)),
            StringComparer.Ordinal);
    }

    private static string NormalizeModuleName(string moduleName)
    {
        return moduleName.NormalizeRequired(nameof(moduleName), "Module name");
    }
}
