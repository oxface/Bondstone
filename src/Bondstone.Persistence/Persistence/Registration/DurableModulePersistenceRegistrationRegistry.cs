using Bondstone.Utility;
using System.ComponentModel;

namespace Bondstone.Persistence;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DurableModulePersistenceRegistrationRegistry
{
    private readonly object _syncRoot = new();
    private readonly List<DurableModuleOutboxWriterRegistration> _outboxWriterRegistrations = [];
    private readonly List<DurableModuleOutboxDispatcherRegistration>
        _outboxDispatcherRegistrations = [];
    private readonly List<DurableModuleOutboxInspectionStoreRegistration>
        _outboxInspectionStoreRegistrations = [];
    private readonly List<DurableModuleInboxHandlerExecutorRegistration>
        _inboxHandlerExecutorRegistrations = [];
    private readonly List<DurableModuleInboxInspectionStoreRegistration>
        _inboxInspectionStoreRegistrations = [];
    private readonly List<DurableModuleIncomingInboxIngestionBoundaryRegistration>
        _incomingInboxIngestionBoundaryRegistrations = [];
    private readonly List<DurableModuleIncomingInboxDispatcherRegistration>
        _incomingInboxDispatcherRegistrations = [];
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

    public IReadOnlyList<DurableModuleOutboxInspectionStoreRegistration>
        OutboxInspectionStoreRegistrations
    {
        get
        {
            lock (_syncRoot)
            {
                return _outboxInspectionStoreRegistrations.ToArray();
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

    public IReadOnlyList<DurableModuleInboxInspectionStoreRegistration>
        InboxInspectionStoreRegistrations
    {
        get
        {
            lock (_syncRoot)
            {
                return _inboxInspectionStoreRegistrations.ToArray();
            }
        }
    }

    public IReadOnlyList<DurableModuleIncomingInboxIngestionBoundaryRegistration>
        IncomingInboxIngestionBoundaryRegistrations
    {
        get
        {
            lock (_syncRoot)
            {
                return _incomingInboxIngestionBoundaryRegistrations.ToArray();
            }
        }
    }

    public IReadOnlyList<DurableModuleIncomingInboxDispatcherRegistration>
        IncomingInboxDispatcherRegistrations
    {
        get
        {
            lock (_syncRoot)
            {
                return _incomingInboxDispatcherRegistrations.ToArray();
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

    public void AddOutboxInspectionStore(
        DurableModuleOutboxInspectionStoreRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        lock (_syncRoot)
        {
            ValidateNoExistingRegistration(
                _outboxInspectionStoreRegistrations,
                registration.ModuleName,
                static existing => existing.ModuleName,
                "durable module outbox inspection store");
            _outboxInspectionStoreRegistrations.Add(registration);
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

    public void AddInboxInspectionStore(
        DurableModuleInboxInspectionStoreRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        lock (_syncRoot)
        {
            ValidateNoExistingRegistration(
                _inboxInspectionStoreRegistrations,
                registration.ModuleName,
                static existing => existing.ModuleName,
                "durable module inbox inspection store");
            _inboxInspectionStoreRegistrations.Add(registration);
        }
    }

    public void AddIncomingInboxIngestionBoundary(
        DurableModuleIncomingInboxIngestionBoundaryRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        lock (_syncRoot)
        {
            ValidateNoExistingRegistration(
                _incomingInboxIngestionBoundaryRegistrations,
                registration.ModuleName,
                static existing => existing.ModuleName,
                "durable module incoming inbox ingestion boundary");
            _incomingInboxIngestionBoundaryRegistrations.Add(registration);
        }
    }

    public void AddIncomingInboxDispatcher(
        DurableModuleIncomingInboxDispatcherRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        lock (_syncRoot)
        {
            ValidateNoExistingRegistration(
                _incomingInboxDispatcherRegistrations,
                registration.ModuleName,
                static existing => existing.ModuleName,
                "durable module incoming inbox dispatcher");
            _incomingInboxDispatcherRegistrations.Add(registration);
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
