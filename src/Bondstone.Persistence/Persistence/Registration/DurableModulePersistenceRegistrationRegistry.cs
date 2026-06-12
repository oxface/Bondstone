using Bondstone.Utility;

namespace Bondstone.Persistence;

public sealed class DurableModulePersistenceRegistrationRegistry
{
    private readonly object _syncRoot = new();
    private readonly List<DurableModuleOutboxWriterRegistration> _outboxWriterRegistrations = [];
    private readonly List<DurableModuleOutboxDispatcherRegistration>
        _outboxDispatcherRegistrations = [];
    private readonly List<DurableModuleInboxHandlerExecutorRegistration>
        _inboxHandlerExecutorRegistrations = [];
    private readonly List<DurableModuleOperationStateStoreRegistration>
        _operationStateStoreRegistrations = [];

    public IReadOnlyList<DurableModuleOutboxWriterRegistration> OutboxWriterRegistrations
    {
        get
        {
            lock (_syncRoot)
            {
                return _outboxWriterRegistrations.ToArray();
            }
        }
    }

    public IReadOnlyList<DurableModuleOutboxDispatcherRegistration>
        OutboxDispatcherRegistrations
    {
        get
        {
            lock (_syncRoot)
            {
                return _outboxDispatcherRegistrations.ToArray();
            }
        }
    }

    public IReadOnlyList<DurableModuleInboxHandlerExecutorRegistration>
        InboxHandlerExecutorRegistrations
    {
        get
        {
            lock (_syncRoot)
            {
                return _inboxHandlerExecutorRegistrations.ToArray();
            }
        }
    }

    public IReadOnlyList<DurableModuleOperationStateStoreRegistration>
        OperationStateStoreRegistrations
    {
        get
        {
            lock (_syncRoot)
            {
                return _operationStateStoreRegistrations.ToArray();
            }
        }
    }

    public void AddOutboxWriter(DurableModuleOutboxWriterRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        lock (_syncRoot)
        {
            ValidateNoExistingRegistration(
                _outboxWriterRegistrations,
                registration.ModuleName,
                static existing => existing.ModuleName,
                "durable module outbox writer");
            _outboxWriterRegistrations.Add(registration);
        }
    }

    public void AddOutboxDispatcher(DurableModuleOutboxDispatcherRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        lock (_syncRoot)
        {
            ValidateNoExistingRegistration(
                _outboxDispatcherRegistrations,
                registration.ModuleName,
                static existing => existing.ModuleName,
                "durable module outbox dispatcher");
            _outboxDispatcherRegistrations.Add(registration);
        }
    }

    public void AddInboxHandlerExecutor(
        DurableModuleInboxHandlerExecutorRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        lock (_syncRoot)
        {
            ValidateNoExistingRegistration(
                _inboxHandlerExecutorRegistrations,
                registration.ModuleName,
                static existing => existing.ModuleName,
                "durable module inbox handler executor");
            _inboxHandlerExecutorRegistrations.Add(registration);
        }
    }

    public void AddOperationStateStore(
        DurableModuleOperationStateStoreRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        lock (_syncRoot)
        {
            ValidateNoExistingRegistration(
                _operationStateStoreRegistrations,
                registration.ModuleName,
                static existing => existing.ModuleName,
                "durable module operation-state store");
            _operationStateStoreRegistrations.Add(registration);
        }
    }

    private static void ValidateNoExistingRegistration<TRegistration>(
        IReadOnlyCollection<TRegistration> registrations,
        string moduleName,
        Func<TRegistration, string> moduleNameSelector,
        string registrationDescription)
    {
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");

        if (!registrations.Any(registration => StringComparer.Ordinal.Equals(
                moduleNameSelector(registration),
                normalizedModuleName)))
        {
            return;
        }

        throw new InvalidOperationException(
            $"A {registrationDescription} registration already exists for module '{normalizedModuleName}'. Configure exactly one {registrationDescription} per module.");
    }
}
