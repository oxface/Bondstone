using Bondstone.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Bondstone.Modules;

internal sealed class ModuleEventSubscriberPipelinePlanner
{
    private readonly IBondstoneModuleRegistry _moduleRegistry;
    private readonly ModulePipelineContributionRegistry _pipelineContributionRegistry;

    public ModuleEventSubscriberPipelinePlanner(
        IBondstoneModuleRegistry moduleRegistry,
        ModulePipelineContributionRegistry pipelineContributionRegistry)
    {
        _moduleRegistry = moduleRegistry ?? throw new ArgumentNullException(nameof(moduleRegistry));
        _pipelineContributionRegistry = pipelineContributionRegistry
            ?? throw new ArgumentNullException(nameof(pipelineContributionRegistry));
    }

    public ModuleEventSubscriberPipelinePlan<TEvent> BuildPlan<TEvent>(
        IServiceProvider serviceProvider,
        ModuleEventSubscriberRegistration subscriber)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(subscriber);

        BondstoneModuleRegistration module = _moduleRegistry.GetModule(subscriber.ModuleName);
        ModuleEventSubscriberPipelineContribution[] selectedRuntimeContributions =
            _pipelineContributionRegistry
                .GetEventSubscriberContributions(module)
                .Where(contribution => contribution.AppliesTo(module))
                .ToArray();
        ValidateNoAmbiguousRuntimeContributions(
            selectedRuntimeContributions,
            module.Name,
            typeof(TEvent));

        IReadOnlyList<ModuleEventSubscriberPipelineStep<TEvent>> runtimeSteps =
            selectedRuntimeContributions
            .OrderBy(static step => step.Order)
            .Select(contribution => ModuleEventSubscriberPipelineStep<TEvent>.Runtime(
                contribution.Kind,
                contribution.Order,
                contribution.CreateBehavior<TEvent>(serviceProvider)))
            .ToArray();
        IReadOnlyList<ModuleEventSubscriberPipelineStep<TEvent>> applicationSteps = serviceProvider
            .GetServices<IModuleEventSubscriberPipelineBehavior<TEvent>>()
            .Select(static behavior => ModuleEventSubscriberPipelineStep<TEvent>.Application(
                behavior))
            .ToArray();

        return new ModuleEventSubscriberPipelinePlan<TEvent>(
            runtimeSteps
                .Concat(applicationSteps)
                .ToArray());
    }

    private static void ValidateNoAmbiguousRuntimeContributions(
        IReadOnlyCollection<ModuleEventSubscriberPipelineContribution> contributions,
        string moduleName,
        Type eventType)
    {
        ModuleEventSubscriberPipelineContribution[] duplicateNameContributions = contributions
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
                $"Module '{moduleName}' has multiple event subscriber runtime pipeline contributions "
                + $"with the same name for event type '{eventType.FullName}': "
                + $"'{duplicateContributionList}'. Runtime contribution names must be unique.");
        }

        ModuleEventSubscriberPipelineContribution[] ambiguousContributions = contributions
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
            $"Module '{moduleName}' has multiple event subscriber runtime pipeline contributions "
            + $"with the same order for event type '{eventType.FullName}': "
            + $"'{contributionList}'. Runtime contribution order must be explicit and unambiguous.");
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
    public static ModuleEventSubscriberPipelineStep<TEvent> Runtime(
        ModulePipelineStepKind kind,
        int order,
        IModuleEventSubscriberPipelineBehavior<TEvent> behavior)
    {
        ArgumentNullException.ThrowIfNull(behavior);
        if (kind == ModulePipelineStepKind.Application)
        {
            throw new ArgumentException(
                "Application pipeline behavior is not a runtime contribution.",
                nameof(kind));
        }

        return new ModuleEventSubscriberPipelineStep<TEvent>(
            kind,
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
