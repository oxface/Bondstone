using Bondstone.Messaging;

namespace Bondstone.Modules;

public delegate ValueTask ModuleEventSubscriberPipelineNext(CancellationToken ct = default);

public interface IModuleEventSubscriberPipelineBehavior<TEvent>
    where TEvent : IIntegrationEvent
{
    ValueTask HandleAsync(
        TEvent integrationEvent,
        ModuleEventSubscriberExecutionContext context,
        ModuleEventSubscriberPipelineNext next,
        CancellationToken ct = default);
}
