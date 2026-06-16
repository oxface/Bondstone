using Bondstone.Modules;
using Bondstone.Messaging;
using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Bondstone.DomainEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bondstone.Persistence.EntityFrameworkCore.DomainEvents;

internal sealed class EntityFrameworkCoreDomainEventModulePostHandlerAction(
    IServiceProvider serviceProvider,
    EntityFrameworkCoreDomainEventModuleRuntimeRegistry moduleRuntimeRegistry)
    : IModulePostHandlerAction
{
    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly EntityFrameworkCoreDomainEventModuleRuntimeRegistry _moduleRuntimeRegistry =
        moduleRuntimeRegistry ?? throw new ArgumentNullException(nameof(moduleRuntimeRegistry));

    public ValueTask RunAsync(
        IModuleRuntimeExecutionContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        EntityFrameworkCoreDomainEventModuleRuntimeDescriptor runtime =
            _moduleRuntimeRegistry.GetRuntime(context.ModuleName);
        if (!runtime.UsesDomainEventPersistence)
        {
            return ValueTask.CompletedTask;
        }

        if (!runtime.UsesEntityFrameworkCorePersistence)
        {
            throw new InvalidOperationException(
                $"Module '{context.ModuleName}' uses Entity Framework Core domain event persistence but has not declared "
                + $"persistence provider '{EntityFrameworkCoreModulePersistence.ProviderName}'. Configure the module with "
                + "UseEntityFrameworkCorePersistence<TDbContext>() before UseEntityFrameworkCoreDomainEventPersistence().");
        }

        BondstoneModuleRegistration module = runtime.Module;
        Type dbContextType = GetDbContextType(module);
        DbContext dbContext = (DbContext)_serviceProvider.GetRequiredService(dbContextType);
        ValidateDomainEventMappings(module, dbContext);

        var collector = new EntityFrameworkCoreDomainEventCollector(
            dbContext,
            module.Name,
            _serviceProvider.GetService<DurablePayloadJsonOptions>(),
            _serviceProvider.GetService<TimeProvider>());

        IReadOnlyList<IDomainEventSource> sources = collector.CollectAndStage();
        if (sources.Count == 0)
        {
            return ValueTask.CompletedTask;
        }

        if (context.ObservesTransactionOutcome)
        {
            context.OnTransactionCommitted(_ =>
            {
                foreach (IDomainEventSource source in sources)
                {
                    source.ClearPendingDomainEvents();
                }

                return ValueTask.CompletedTask;
            });
        }

        return ValueTask.CompletedTask;
    }

    private static void ValidateDomainEventMappings(
        BondstoneModuleRegistration module,
        DbContext dbContext)
    {
        if (dbContext.Model.FindEntityType(typeof(DomainEventRecordEntity)) is not null)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Module '{module.Name}' uses Entity Framework Core domain event persistence with context "
            + $"'{dbContext.GetType().FullName}', but the DbContext model is missing the Bondstone EF Core "
            + "domain event mapping. Map domain events explicitly with ApplyBondstoneDomainEvents().");
    }

    private static Type GetDbContextType(BondstoneModuleRegistration module)
    {
        Type? contextType = module.PersistenceContextType;
        if (contextType is null)
        {
            throw new InvalidOperationException(
                $"Module '{module.Name}' uses Entity Framework Core domain event persistence but has no DbContext type.");
        }

        if (!typeof(DbContext).IsAssignableFrom(contextType))
        {
            throw new InvalidOperationException(
                $"Module '{module.Name}' uses Entity Framework Core domain event persistence context '{contextType.FullName}', which must derive from '{typeof(DbContext).FullName}'.");
        }

        return contextType;
    }
}
