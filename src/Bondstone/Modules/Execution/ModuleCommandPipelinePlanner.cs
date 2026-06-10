using Bondstone.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Bondstone.Modules;

internal sealed class ModuleCommandPipelinePlanner
{
    public ModuleCommandPipelinePlan<TCommand> BuildPlan<TCommand>(
        IServiceProvider serviceProvider,
        ModuleCommandRoute route)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(route);

        IReadOnlyList<ModuleCommandPipelineStep<TCommand>> systemSteps = serviceProvider
            .GetServices<IModuleCommandSystemPipelineBehavior<TCommand>>()
            .OrderBy(static behavior => behavior.Order)
            .Select(static behavior => ModuleCommandPipelineStep<TCommand>.System(
                behavior.Order,
                behavior))
            .ToArray();
        IReadOnlyList<ModuleCommandPipelineStep<TCommand>> applicationSteps = serviceProvider
            .GetServices<IModuleCommandPipelineBehavior<TCommand>>()
            .Select(static behavior => ModuleCommandPipelineStep<TCommand>.Application(
                behavior))
            .ToArray();

        return new ModuleCommandPipelinePlan<TCommand>(
            systemSteps
                .Concat(applicationSteps)
                .ToArray());
    }
}

internal sealed record ModuleCommandPipelinePlan<TCommand>(
    IReadOnlyList<ModuleCommandPipelineStep<TCommand>> Steps)
    where TCommand : ICommand;

internal sealed record ModuleCommandPipelineStep<TCommand>(
    ModulePipelineStepKind Kind,
    int? Order,
    IModuleCommandPipelineBehavior<TCommand> Behavior)
    where TCommand : ICommand
{
    public static ModuleCommandPipelineStep<TCommand> System(
        int order,
        IModuleCommandSystemPipelineBehavior<TCommand> behavior)
    {
        ArgumentNullException.ThrowIfNull(behavior);

        return new ModuleCommandPipelineStep<TCommand>(
            ModulePipelineStepKind.System,
            order,
            behavior);
    }

    public static ModuleCommandPipelineStep<TCommand> Application(
        IModuleCommandPipelineBehavior<TCommand> behavior)
    {
        ArgumentNullException.ThrowIfNull(behavior);

        return new ModuleCommandPipelineStep<TCommand>(
            ModulePipelineStepKind.Application,
            null,
            behavior);
    }
}
