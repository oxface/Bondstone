using Bondstone.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Bondstone.Modules;

internal sealed class ModuleEventSubscriberPipelinePlanner
{
    public ModuleEventSubscriberPipelinePlan<TEvent> BuildPlan<TEvent>(
        IServiceProvider serviceProvider,
        ModuleEventSubscriberRegistration subscriber)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(subscriber);

        IReadOnlyList<ModuleEventSubscriberPipelineStep<TEvent>> systemSteps = serviceProvider
            .GetServices<IModuleEventSubscriberSystemPipelineBehavior<TEvent>>()
            .OrderBy(static behavior => behavior.Order)
            .Select(static behavior => ModuleEventSubscriberPipelineStep<TEvent>.System(
                behavior.Order,
                behavior))
            .ToArray();
        IReadOnlyList<ModuleEventSubscriberPipelineStep<TEvent>> applicationSteps = serviceProvider
            .GetServices<IModuleEventSubscriberPipelineBehavior<TEvent>>()
            .Select(static behavior => ModuleEventSubscriberPipelineStep<TEvent>.Application(
                behavior))
            .ToArray();

        return new ModuleEventSubscriberPipelinePlan<TEvent>(
            systemSteps
                .Concat(applicationSteps)
                .ToArray());
    }
}

internal sealed record ModuleEventSubscriberPipelinePlan<TEvent>(
    IReadOnlyList<ModuleEventSubscriberPipelineStep<TEvent>> Steps)
    where TEvent : IIntegrationEvent;

internal sealed record ModuleEventSubscriberPipelineStep<TEvent>(
    ModulePipelineStepKind Kind,
    int? Order,
    IModuleEventSubscriberPipelineBehavior<TEvent> Behavior)
    where TEvent : IIntegrationEvent
{
    public static ModuleEventSubscriberPipelineStep<TEvent> System(
        int order,
        IModuleEventSubscriberSystemPipelineBehavior<TEvent> behavior)
    {
        ArgumentNullException.ThrowIfNull(behavior);

        return new ModuleEventSubscriberPipelineStep<TEvent>(
            ModulePipelineStepKind.System,
            order,
            behavior);
    }

    public static ModuleEventSubscriberPipelineStep<TEvent> Application(
        IModuleEventSubscriberPipelineBehavior<TEvent> behavior)
    {
        ArgumentNullException.ThrowIfNull(behavior);

        return new ModuleEventSubscriberPipelineStep<TEvent>(
            ModulePipelineStepKind.Application,
            null,
            behavior);
    }
}
