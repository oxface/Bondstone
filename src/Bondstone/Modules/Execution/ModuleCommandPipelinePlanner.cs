using Bondstone.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Bondstone.Modules;

internal sealed class ModuleCommandPipelinePlanner
{
    private readonly IBondstoneModuleRegistry _moduleRegistry;
    private readonly ModulePipelineContributionRegistry _pipelineContributionRegistry;

    public ModuleCommandPipelinePlanner(
        IBondstoneModuleRegistry moduleRegistry,
        ModulePipelineContributionRegistry pipelineContributionRegistry)
    {
        _moduleRegistry = moduleRegistry ?? throw new ArgumentNullException(nameof(moduleRegistry));
        _pipelineContributionRegistry = pipelineContributionRegistry
            ?? throw new ArgumentNullException(nameof(pipelineContributionRegistry));
    }

    public ModuleCommandPipelinePlan<TCommand> BuildPlan<TCommand>(
        IServiceProvider serviceProvider,
        ModuleCommandRoute route)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(route);

        BondstoneModuleRegistration module = _moduleRegistry.GetModule(route.ModuleName);
        ModuleCommandPipelineContribution[] selectedRuntimeContributions =
            _pipelineContributionRegistry
                .GetCommandContributions(module)
                .Where(contribution => contribution.AppliesTo(module))
                .ToArray();
        ValidateNoAmbiguousRuntimeContributions(
            selectedRuntimeContributions,
            module.Name,
            typeof(TCommand));

        IReadOnlyList<ModuleCommandPipelineStep<TCommand>> runtimeSteps =
            selectedRuntimeContributions
            .OrderBy(static step => step.Order)
            .Select(contribution => ModuleCommandPipelineStep<TCommand>.Runtime(
                contribution.Kind,
                contribution.Order,
                contribution.CreateBehavior<TCommand>(serviceProvider)))
            .ToArray();
        IReadOnlyList<ModuleCommandPipelineStep<TCommand>> applicationSteps = serviceProvider
            .GetServices<IModuleCommandPipelineBehavior<TCommand>>()
            .Select(static behavior => ModuleCommandPipelineStep<TCommand>.Application(
                behavior))
            .ToArray();

        return new ModuleCommandPipelinePlan<TCommand>(
            runtimeSteps
                .Concat(applicationSteps)
                .ToArray());
    }

    private static void ValidateNoAmbiguousRuntimeContributions(
        IReadOnlyCollection<ModuleCommandPipelineContribution> contributions,
        string moduleName,
        Type commandType)
    {
        ModuleCommandPipelineContribution[] duplicateNameContributions = contributions
            .GroupBy(static contribution => contribution.Name, StringComparer.Ordinal)
            .Where(static group => group.Count() > 1)
            .SelectMany(static group => group)
            .OrderBy(static contribution => contribution.Name, StringComparer.Ordinal)
            .ThenBy(static contribution => contribution.Order)
            .ToArray();

        if (duplicateNameContributions.Length > 0)
        {
            string duplicateContributionList = string.Join(
                "', '",
                duplicateNameContributions.Select(static contribution => contribution.Name));

            throw new InvalidOperationException(
                $"Module '{moduleName}' has multiple command runtime pipeline contributions "
                + $"with the same name for command type '{commandType.FullName}': "
                + $"'{duplicateContributionList}'. Runtime contribution names must be unique.");
        }

        ModuleCommandPipelineContribution[] ambiguousContributions = contributions
            .GroupBy(static contribution => contribution.Order)
            .Where(static group => group.Count() > 1)
            .SelectMany(static group => group)
            .OrderBy(static contribution => contribution.Kind)
            .ThenBy(static contribution => contribution.Order)
            .ThenBy(static contribution => contribution.Name, StringComparer.Ordinal)
            .ToArray();

        if (ambiguousContributions.Length == 0)
        {
            return;
        }

        string contributionList = string.Join(
            "', '",
            ambiguousContributions.Select(static contribution => contribution.Name));

        throw new InvalidOperationException(
            $"Module '{moduleName}' has multiple command runtime pipeline contributions "
            + $"with the same order for command type '{commandType.FullName}': "
            + $"'{contributionList}'. Runtime contribution order must be explicit and unambiguous.");
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
    public static ModuleCommandPipelineStep<TCommand> Runtime(
        ModulePipelineStepKind kind,
        int order,
        IModuleCommandPipelineBehavior<TCommand> behavior)
    {
        ArgumentNullException.ThrowIfNull(behavior);
        if (kind == ModulePipelineStepKind.Application)
        {
            throw new ArgumentException(
                "Application pipeline behavior is not a runtime contribution.",
                nameof(kind));
        }

        return new ModuleCommandPipelineStep<TCommand>(
            kind,
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
