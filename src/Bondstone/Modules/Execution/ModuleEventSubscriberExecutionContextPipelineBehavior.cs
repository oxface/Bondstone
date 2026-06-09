using Bondstone.Messaging;

namespace Bondstone.Modules;

internal sealed class ModuleEventSubscriberExecutionContextPipelineBehavior<TEvent>(
    ModuleExecutionContextAccessor executionContextAccessor)
    : IModuleEventSubscriberSystemPipelineBehavior<TEvent>
    where TEvent : IIntegrationEvent
{
    private readonly ModuleExecutionContextAccessor _executionContextAccessor =
        executionContextAccessor ?? throw new ArgumentNullException(nameof(executionContextAccessor));

    public int Order => ModuleEventSubscriberSystemPipelineOrder.ExecutionContext;

    public async ValueTask HandleAsync(
        TEvent integrationEvent,
        ModuleEventSubscriberExecutionContext context,
        ModuleEventSubscriberPipelineNext next,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        using IDisposable scope = _executionContextAccessor.Push(
            new ModuleExecutionContext(context.ModuleName));

        await next(ct);
    }
}
