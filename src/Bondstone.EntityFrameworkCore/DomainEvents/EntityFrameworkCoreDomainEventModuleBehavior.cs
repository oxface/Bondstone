using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.EntityFrameworkCore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bondstone.EntityFrameworkCore.DomainEvents;

internal sealed class EntityFrameworkCoreDomainEventModuleCommandBehavior<TCommand>(
    IServiceProvider serviceProvider,
    IBondstoneModuleRegistry moduleRegistry,
    EntityFrameworkCoreDomainEventTransactionState transactionState)
    : IModuleCommandSystemPipelineBehavior<TCommand>
    where TCommand : ICommand
{
    private readonly EntityFrameworkCoreDomainEventModuleBehaviorCore _core = new(
        serviceProvider,
        moduleRegistry,
        transactionState);

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
            context.ModuleName,
            nextCt => next(nextCt),
            ct);
    }
}

internal sealed class EntityFrameworkCoreDomainEventModuleEventSubscriberBehavior<TEvent>(
    IServiceProvider serviceProvider,
    IBondstoneModuleRegistry moduleRegistry,
    EntityFrameworkCoreDomainEventTransactionState transactionState)
    : IModuleEventSubscriberSystemPipelineBehavior<TEvent>
    where TEvent : IIntegrationEvent
{
    private readonly EntityFrameworkCoreDomainEventModuleBehaviorCore _core = new(
        serviceProvider,
        moduleRegistry,
        transactionState);

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
            context.ModuleName,
            nextCt => next(nextCt),
            ct);
    }
}

internal sealed class EntityFrameworkCoreDomainEventModuleBehaviorCore(
    IServiceProvider serviceProvider,
    IBondstoneModuleRegistry moduleRegistry,
    EntityFrameworkCoreDomainEventTransactionState transactionState)
{
    public const int Order = ModuleCommandSystemPipelineOrder.ExecutionContext + 10;

    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly IBondstoneModuleRegistry _moduleRegistry =
        moduleRegistry ?? throw new ArgumentNullException(nameof(moduleRegistry));
    private readonly EntityFrameworkCoreDomainEventTransactionState _transactionState =
        transactionState ?? throw new ArgumentNullException(nameof(transactionState));

    public async ValueTask HandleAsync(
        string moduleName,
        Func<CancellationToken, ValueTask> next,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (!ShouldCollect(moduleName, out BondstoneModuleRegistration? module))
        {
            await next(ct);
            return;
        }

        Type dbContextType = EntityFrameworkCoreModuleTransactionRunner.GetDbContextType(module);
        DbContext dbContext = (DbContext)_serviceProvider.GetRequiredService(dbContextType);
        ValidateDomainEventMappings(module, dbContext);

        await next(ct);

        var collector = new EntityFrameworkCoreDomainEventCollector(
            dbContext,
            module.Name,
            _serviceProvider.GetService<DurablePayloadJsonOptions>(),
            _serviceProvider.GetService<TimeProvider>());

        _transactionState.AddCollectedSources(
            module.Name,
            collector.CollectAndStage());
    }

    private bool ShouldCollect(
        string moduleName,
        out BondstoneModuleRegistration module)
    {
        module = _moduleRegistry.GetModule(moduleName);
        if (!module.UsesPersistence
            || !StringComparer.Ordinal.Equals(
                module.PersistenceProviderName,
                Persistence.EntityFrameworkCoreModulePersistence.ProviderName))
        {
            return false;
        }

        string normalizedModuleName = module.Name;
        return _serviceProvider
            .GetServices<EntityFrameworkCoreDomainEventPersistenceModule>()
            .Any(registration => StringComparer.Ordinal.Equals(
                registration.ModuleName,
                normalizedModuleName));
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
            + "domain event mapping. Map domain events with ApplyBondstoneDomainEvents(), or use "
            + "ApplyBondstonePersistence().");
    }
}
