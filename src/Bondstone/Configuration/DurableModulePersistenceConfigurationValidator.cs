using Bondstone.Modules;
using Bondstone.Persistence;

namespace Bondstone.Configuration;

internal sealed class DurableModulePersistenceConfigurationValidator(
    DurableModulePersistenceRegistrationRegistry persistenceRegistrationRegistry)
    : IBondstoneConfigurationValidator
{
    private readonly DurableModulePersistenceRegistrationRegistry _persistenceRegistrationRegistry =
        persistenceRegistrationRegistry
        ?? throw new ArgumentNullException(nameof(persistenceRegistrationRegistry));

    public void Validate(BondstoneConfigurationValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        DurableModuleOutboxWriterRegistration[] outboxWriters =
            _persistenceRegistrationRegistry.OutboxWriterRegistrations.ToArray();
        DurableModuleInboxHandlerExecutorRegistration[] inboxExecutors =
            _persistenceRegistrationRegistry.InboxHandlerExecutorRegistrations.ToArray();
        DurableModuleInboxInspectionStoreRegistration[] inboxInspectionStores =
            _persistenceRegistrationRegistry.InboxInspectionStoreRegistrations.ToArray();
        DurableModuleIncomingInboxIngestionBoundaryRegistration[] incomingInboxIngestionBoundaries =
            _persistenceRegistrationRegistry.IncomingInboxIngestionBoundaryRegistrations.ToArray();
        DurableModuleIncomingInboxDispatcherRegistration[] incomingInboxDispatchers =
            _persistenceRegistrationRegistry.IncomingInboxDispatcherRegistrations.ToArray();
        DurableModuleOperationStateStoreRegistration[] operationStateStores =
            _persistenceRegistrationRegistry.OperationStateStoreRegistrations.ToArray();
        DurableModuleOutboxDispatcherRegistration[] outboxDispatchers =
            _persistenceRegistrationRegistry.OutboxDispatcherRegistrations.ToArray();
        DurableModuleOutboxInspectionStoreRegistration[] outboxInspectionStores =
            _persistenceRegistrationRegistry.OutboxInspectionStoreRegistrations.ToArray();

        bool hasDurableModuleRoleRegistrations =
            outboxWriters.Length > 0
            || inboxExecutors.Length > 0
            || inboxInspectionStores.Length > 0
            || incomingInboxDispatchers.Length > 0
            || operationStateStores.Length > 0
            || outboxDispatchers.Length > 0
            || outboxInspectionStores.Length > 0;

        if (!hasDurableModuleRoleRegistrations
            && incomingInboxIngestionBoundaries.Length == 0)
        {
            return;
        }

        ValidateRegisteredModulesExist(
            context,
            outboxWriters.Select(static registration => registration.ModuleName)
                .Concat(inboxExecutors.Select(static registration => registration.ModuleName))
                .Concat(inboxInspectionStores.Select(static registration => registration.ModuleName))
                .Concat(incomingInboxIngestionBoundaries.Select(static registration =>
                    registration.ModuleName))
                .Concat(incomingInboxDispatchers.Select(static registration => registration.ModuleName))
                .Concat(operationStateStores.Select(static registration => registration.ModuleName))
                .Concat(outboxDispatchers.Select(static registration => registration.ModuleName))
                .Concat(outboxInspectionStores.Select(static registration => registration.ModuleName)));

        if (!hasDurableModuleRoleRegistrations)
        {
            return;
        }

        HashSet<string> modulesWithOutboxWriter = outboxWriters
            .Select(static registration => registration.ModuleName)
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> modulesWithInboxExecutor = inboxExecutors
            .Select(static registration => registration.ModuleName)
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> modulesWithIncomingInboxDispatcher = incomingInboxDispatchers
            .Select(static registration => registration.ModuleName)
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> modulesWithOperationStateStore = operationStateStores
            .Select(static registration => registration.ModuleName)
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> modulesWithOutboxDispatcher = outboxDispatchers
            .Select(static registration => registration.ModuleName)
            .ToHashSet(StringComparer.Ordinal);

        foreach (BondstoneModuleRegistration module in context.Modules.Where(static module =>
                     module.UsesDurableMessaging
                     && module.UsesPersistence))
        {
            var missingRoles = new List<string>(capacity: 4);
            if (!modulesWithOutboxWriter.Contains(module.Name))
            {
                missingRoles.Add("outbox writer");
            }

            if (!modulesWithInboxExecutor.Contains(module.Name))
            {
                missingRoles.Add("inbox handler executor");
            }

            if (!modulesWithIncomingInboxDispatcher.Contains(module.Name))
            {
                missingRoles.Add("incoming inbox dispatcher");
            }

            if (!modulesWithOperationStateStore.Contains(module.Name))
            {
                missingRoles.Add("operation-state store");
            }

            if (!modulesWithOutboxDispatcher.Contains(module.Name))
            {
                missingRoles.Add("outbox dispatcher");
            }

            if (missingRoles.Count == 0)
            {
                continue;
            }

            string providerName = module.PersistenceProviderName ?? "(unknown provider)";
            throw new InvalidOperationException(
                $"Module '{module.Name}' declares durable messaging with persistence provider '{providerName}' but is missing durable module persistence role registrations: {string.Join(", ", missingRoles)}. Configure module persistence services through provider-specific module helpers so all durable module roles are registered.");
        }
    }

    private static void ValidateRegisteredModulesExist(
        BondstoneConfigurationValidationContext context,
        IEnumerable<string> moduleNames)
    {
        string[] unknownModuleNames = moduleNames
            .Distinct(StringComparer.Ordinal)
            .Where(moduleName => !context.ModulesByName.ContainsKey(moduleName))
            .OrderBy(static moduleName => moduleName, StringComparer.Ordinal)
            .ToArray();

        if (unknownModuleNames.Length == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Durable module persistence registrations exist for unknown module(s): '{string.Join("', '", unknownModuleNames)}'. Register the module in AddBondstone before registering module-specific durable persistence roles.");
    }
}
