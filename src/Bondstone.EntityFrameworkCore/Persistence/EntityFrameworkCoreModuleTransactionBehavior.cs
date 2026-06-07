using System.Collections.Concurrent;
using Bondstone.EntityFrameworkCore.Inbox;
using Bondstone.EntityFrameworkCore.Outbox;
using Bondstone.Messaging;
using Bondstone.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bondstone.EntityFrameworkCore.Persistence;

internal sealed class EntityFrameworkCoreModuleTransactionBehavior<TCommand>(
    IServiceProvider serviceProvider,
    IBondstoneModuleRegistry moduleRegistry)
    : IModuleCommandSystemPipelineBehavior<TCommand>
    where TCommand : ICommand
{
    private static readonly ConcurrentDictionary<Type, ObjectFactory> ScopeFactories = new();
    private static readonly object[] EmptyArguments = [];

    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly IBondstoneModuleRegistry _moduleRegistry =
        moduleRegistry ?? throw new ArgumentNullException(nameof(moduleRegistry));

    public int Order => ModuleCommandSystemPipelineOrder.Transaction;

    public async ValueTask HandleAsync(
        TCommand command,
        ModuleCommandExecutionContext context,
        ModuleCommandPipelineNext next,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        BondstoneModuleRegistration module = _moduleRegistry.GetModule(context.ModuleName);
        if (!module.UsesPersistence
            || !StringComparer.Ordinal.Equals(
                module.PersistenceProviderName,
                EntityFrameworkCoreModulePersistence.ProviderName))
        {
            await next(ct);
            return;
        }

        Type dbContextType = GetDbContextType(module);
        DbContext dbContext = (DbContext)_serviceProvider.GetRequiredService(dbContextType);
        ValidateDurableMessagingMappings(module, dbContext);

        IEntityFrameworkCorePersistenceScope persistenceScope = CreatePersistenceScope(dbContextType);

        await persistenceScope.ExecuteAsync(
            async (scope, scopeCt) =>
            {
                await next(scopeCt);
                await scope.SaveChangesAsync(scopeCt);
            },
            ct);
    }

    private static Type GetDbContextType(BondstoneModuleRegistration module)
    {
        Type? contextType = module.PersistenceContextType;
        if (contextType is null)
        {
            throw new InvalidOperationException(
                $"Module '{module.Name}' uses Entity Framework Core persistence but has no DbContext type.");
        }

        if (!typeof(DbContext).IsAssignableFrom(contextType))
        {
            throw new InvalidOperationException(
                $"Module '{module.Name}' uses Entity Framework Core persistence context '{contextType.FullName}', which must derive from '{typeof(DbContext).FullName}'.");
        }

        return contextType;
    }

    private static void ValidateDurableMessagingMappings(
        BondstoneModuleRegistration module,
        DbContext dbContext)
    {
        if (!module.UsesDurableMessaging)
        {
            return;
        }

        List<string> missingMappings = [];
        if (dbContext.Model.FindEntityType(typeof(OutboxMessageEntity)) is null)
        {
            missingMappings.Add("outbox");
        }

        if (dbContext.Model.FindEntityType(typeof(InboxMessageEntity)) is null)
        {
            missingMappings.Add("inbox");
        }

        if (missingMappings.Count == 0)
        {
            return;
        }

        string joinedMappings = string.Join(", ", missingMappings);
        throw new InvalidOperationException(
            $"Module '{module.Name}' uses durable messaging with Entity Framework Core persistence context "
            + $"'{dbContext.GetType().FullName}', but the DbContext model is missing required Bondstone EF Core mappings: "
            + $"{joinedMappings}. Map the required durable messaging tables with ApplyBondstoneOutbox() "
            + "and ApplyBondstoneInbox(), or use ApplyBondstonePersistence().");
    }

    private IEntityFrameworkCorePersistenceScope CreatePersistenceScope(Type dbContextType)
    {
        ObjectFactory factory = ScopeFactories.GetOrAdd(
            dbContextType,
            static type => ActivatorUtilities.CreateFactory(
                typeof(EntityFrameworkCorePersistenceScope<>).MakeGenericType(type),
                Type.EmptyTypes));

        return (IEntityFrameworkCorePersistenceScope)factory(_serviceProvider, EmptyArguments);
    }
}
