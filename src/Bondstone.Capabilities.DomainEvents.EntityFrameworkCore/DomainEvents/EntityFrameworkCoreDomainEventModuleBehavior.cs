using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Bondstone.Capabilities.DomainEvents;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bondstone.Capabilities.DomainEvents.EntityFrameworkCore.DomainEvents;

internal sealed class EntityFrameworkCoreDomainEventModuleCommandBehavior<TCommand>(
    IServiceProvider serviceProvider,
    EntityFrameworkCoreDomainEventModuleRuntimeRegistry moduleRuntimeRegistry)
    : IModuleCommandSystemPipelineBehavior<TCommand>
    where TCommand : ICommand
{
    private readonly EntityFrameworkCoreDomainEventModuleBehaviorCore _core = new(
        serviceProvider,
        moduleRuntimeRegistry);

    public int Order => EntityFrameworkCoreDomainEventModuleBehaviorCore.Order;

    public async ValueTask HandleAsync(
        TCommand command,
        ModuleCommandExecutionContext context,
        ModuleCommandPipelineNext next,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        await _core.HandleAsync(
            context,
            nextCt => next(nextCt),
            ct);
    }
}

internal sealed class EntityFrameworkCoreDomainEventModuleEventSubscriberBehavior<TEvent>(
    IServiceProvider serviceProvider,
    EntityFrameworkCoreDomainEventModuleRuntimeRegistry moduleRuntimeRegistry)
    : IModuleEventSubscriberSystemPipelineBehavior<TEvent>
    where TEvent : IIntegrationEvent
{
    private readonly EntityFrameworkCoreDomainEventModuleBehaviorCore _core = new(
        serviceProvider,
        moduleRuntimeRegistry);

    public int Order => EntityFrameworkCoreDomainEventModuleBehaviorCore.Order;

    public async ValueTask HandleAsync(
        TEvent integrationEvent,
        ModuleEventSubscriberExecutionContext context,
        ModuleEventSubscriberPipelineNext next,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        await _core.HandleAsync(
            context,
            nextCt => next(nextCt),
            ct);
    }
}

internal sealed class EntityFrameworkCoreDomainEventModuleBehaviorCore(
    IServiceProvider serviceProvider,
    EntityFrameworkCoreDomainEventModuleRuntimeRegistry moduleRuntimeRegistry)
{
    public const int Order = ModuleCommandSystemPipelineOrder.ExecutionContext + 10;

    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly EntityFrameworkCoreDomainEventModuleRuntimeRegistry _moduleRuntimeRegistry =
        moduleRuntimeRegistry ?? throw new ArgumentNullException(nameof(moduleRuntimeRegistry));

    public async ValueTask HandleAsync(
        IModulePipelineExecutionContext context,
        Func<CancellationToken, ValueTask> next,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (!ShouldCollect(
                context.ModuleName,
                out EntityFrameworkCoreDomainEventModuleRuntimeDescriptor runtime))
        {
            await next(ct);
            return;
        }

        BondstoneModuleRegistration module = runtime.Module;
        Type dbContextType = GetDbContextType(module);
        DbContext dbContext = (DbContext)_serviceProvider.GetRequiredService(dbContextType);
        ValidateDomainEventMappings(module, dbContext);

        using IDisposable sourceFeatureScope = context.Features.Push<IDomainEventSourceFeature>(
            new EntityFrameworkCoreDomainEventSourceFeature(dbContext));

        await next(ct);

        var collector = new EntityFrameworkCoreDomainEventCollector(
            dbContext,
            module.Name,
            _serviceProvider.GetService<DurablePayloadJsonOptions>(),
            _serviceProvider.GetService<TimeProvider>());

        IReadOnlyList<IDomainEventSource> sources = collector.CollectAndStage();
        if (sources.Count == 0)
        {
            return;
        }

        if (context.Features.TryGet(
                out IModuleTransactionFeature? transactionFeature)
            && transactionFeature is { ObservesCommit: true })
        {
            transactionFeature.OnCommitted(_ =>
            {
                foreach (IDomainEventSource source in sources)
                {
                    source.ClearPendingDomainEvents();
                }

                return ValueTask.CompletedTask;
            });
        }
    }

    private bool ShouldCollect(
        string moduleName,
        out EntityFrameworkCoreDomainEventModuleRuntimeDescriptor runtime)
    {
        runtime = _moduleRuntimeRegistry.GetRuntime(moduleName);
        return runtime.UsesEntityFrameworkCorePersistence
            && runtime.UsesDomainEventPersistence;
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
