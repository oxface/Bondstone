using System.Collections.Concurrent;
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
