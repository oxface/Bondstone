using Bondstone.Messaging;

namespace Bondstone.Modules;

public interface IModuleEventSubscriberSystemPipelineBehavior<TEvent>
    : IModuleEventSubscriberPipelineBehavior<TEvent>
    where TEvent : IIntegrationEvent
{
    int Order { get; }
}
