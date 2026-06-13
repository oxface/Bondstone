using Bondstone.Modules;

namespace Bondstone.Capabilities.DomainEvents.EntityFrameworkCore.DomainEvents;

internal static class EntityFrameworkCoreDomainEventModulePipelineOrder
{
    public const int Command =
        ModuleCommandSystemPipelineOrder.ExecutionContext + 10;

    public const int EventSubscriber =
        ModuleEventSubscriberSystemPipelineOrder.ExecutionContext + 10;
}
