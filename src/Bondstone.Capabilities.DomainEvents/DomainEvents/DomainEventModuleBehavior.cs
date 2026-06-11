using System.Reflection;
using Bondstone.Messaging;
using Bondstone.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Bondstone.Capabilities.DomainEvents;

internal sealed class DomainEventModuleCommandBehavior<TCommand>(
    IServiceProvider serviceProvider,
    DomainEventDispatchModuleRuntimeRegistry moduleRuntimeRegistry)
    : IModuleCommandSystemPipelineBehavior<TCommand>
    where TCommand : ICommand
{
    private readonly DomainEventModuleBehaviorCore _core = new(
        serviceProvider,
        moduleRuntimeRegistry);

    public int Order => DomainEventModuleBehaviorCore.Order;

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

internal sealed class DomainEventModuleEventSubscriberBehavior<TEvent>(
    IServiceProvider serviceProvider,
    DomainEventDispatchModuleRuntimeRegistry moduleRuntimeRegistry)
    : IModuleEventSubscriberSystemPipelineBehavior<TEvent>
    where TEvent : IIntegrationEvent
{
    private readonly DomainEventModuleBehaviorCore _core = new(
        serviceProvider,
        moduleRuntimeRegistry);

    public int Order => DomainEventModuleBehaviorCore.Order;

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

internal sealed class DomainEventModuleBehaviorCore(
    IServiceProvider serviceProvider,
    DomainEventDispatchModuleRuntimeRegistry moduleRuntimeRegistry)
{
    public const int Order = ModuleCommandSystemPipelineOrder.ExecutionContext + 20;

    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly DomainEventDispatchModuleRuntimeRegistry _moduleRuntimeRegistry =
        moduleRuntimeRegistry ?? throw new ArgumentNullException(nameof(moduleRuntimeRegistry));

    public async ValueTask HandleAsync(
        IModulePipelineExecutionContext context,
        Func<CancellationToken, ValueTask> next,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        DomainEventDispatchModuleRuntimeDescriptor runtime =
            _moduleRuntimeRegistry.GetRuntime(context.ModuleName);
        if (!runtime.UsesDomainEventDispatch)
        {
            await next(ct);
            return;
        }

        await next(ct);

        if (!context.Features.TryGet(out IDomainEventSourceFeature? sourceFeature)
            || sourceFeature is null)
        {
            return;
        }

        await DispatchPendingDomainEventsAsync(sourceFeature, ct);
    }

    private async ValueTask DispatchPendingDomainEventsAsync(
        IDomainEventSourceFeature sourceFeature,
        CancellationToken ct)
    {
        HashSet<IDomainEvent> dispatchedDomainEvents = new(ReferenceEqualityComparer.Instance);

        while (true)
        {
            IReadOnlyList<IDomainEvent> pendingDomainEvents = sourceFeature
                .GetPendingDomainEventSources()
                .SelectMany(static source => source.PendingDomainEvents)
                .Where(domainEvent => !dispatchedDomainEvents.Contains(domainEvent))
                .ToArray();

            if (pendingDomainEvents.Count == 0)
            {
                return;
            }

            foreach (IDomainEvent domainEvent in pendingDomainEvents)
            {
                dispatchedDomainEvents.Add(domainEvent);
                await DispatchDomainEventAsync(domainEvent, ct);
            }
        }
    }

    private async ValueTask DispatchDomainEventAsync(
        IDomainEvent domainEvent,
        CancellationToken ct)
    {
        Type handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
        MethodInfo handleMethod = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))
            ?? throw new InvalidOperationException(
                $"Domain event handler type '{handlerType.FullName}' is missing {nameof(IDomainEventHandler<IDomainEvent>.HandleAsync)}.");

        object[] arguments = [domainEvent, ct];
        foreach (object? handler in _serviceProvider.GetServices(handlerType))
        {
            if (handler is null)
            {
                continue;
            }

            if (handleMethod.Invoke(handler, arguments) is not ValueTask handling)
            {
                throw new InvalidOperationException(
                    $"Domain event handler '{handler.GetType().FullName}' did not return a {nameof(ValueTask)}.");
            }

            await handling;
        }
    }
}
