using Bondstone.Modules;

namespace Bondstone.Configuration;

internal sealed class ModuleRuntimePipelineConfigurationValidator(
    ModulePipelineContributionRegistry pipelineContributionRegistry)
    : IBondstoneConfigurationValidator
{
    private readonly ModulePipelineContributionRegistry _pipelineContributionRegistry =
        pipelineContributionRegistry ?? throw new ArgumentNullException(nameof(pipelineContributionRegistry));

    public void Validate(BondstoneConfigurationValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (BondstoneModuleRegistration module in context.Modules)
        {
            if (context.CommandRoutes.Any(route =>
                    StringComparer.Ordinal.Equals(route.ModuleName, module.Name)))
            {
                ValidateCommandContributions(module);
            }

            if (context.EventSubscribers.Any(subscriber =>
                    StringComparer.Ordinal.Equals(subscriber.ModuleName, module.Name)))
            {
                ValidateEventSubscriberContributions(module);
            }
        }
    }

    private void ValidateCommandContributions(BondstoneModuleRegistration module)
    {
        ModuleCommandPipelineContribution[] selectedContributions =
            _pipelineContributionRegistry
                .GetCommandContributions(module)
                .Where(contribution => contribution.AppliesTo(module))
                .ToArray();

        string[] duplicateNames = selectedContributions
            .GroupBy(static contribution => contribution.Name, StringComparer.Ordinal)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        if (duplicateNames.Length > 0)
        {
            throw new InvalidOperationException(
                $"Module '{module.Name}' has multiple command runtime pipeline contributions with the same name: '{string.Join("', '", duplicateNames)}'. Runtime contribution names must be unique.");
        }

        ModuleCommandPipelineContribution[] ambiguousOrderContributions = selectedContributions
            .GroupBy(static contribution => contribution.Order)
            .Where(static group => group.Count() > 1)
            .SelectMany(static group => group)
            .OrderBy(static contribution => contribution.Order)
            .ThenBy(static contribution => contribution.Name, StringComparer.Ordinal)
            .ToArray();

        if (ambiguousOrderContributions.Length == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Module '{module.Name}' has multiple command runtime pipeline contributions with the same order: '{string.Join("', '", ambiguousOrderContributions.Select(static contribution => contribution.Name))}'. Runtime contribution order must be explicit and unambiguous.");
    }

    private void ValidateEventSubscriberContributions(BondstoneModuleRegistration module)
    {
        ModuleEventSubscriberPipelineContribution[] selectedContributions =
            _pipelineContributionRegistry
                .GetEventSubscriberContributions(module)
                .Where(contribution => contribution.AppliesTo(module))
                .ToArray();

        string[] duplicateNames = selectedContributions
            .GroupBy(static contribution => contribution.Name, StringComparer.Ordinal)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        if (duplicateNames.Length > 0)
        {
            throw new InvalidOperationException(
                $"Module '{module.Name}' has multiple event subscriber runtime pipeline contributions with the same name: '{string.Join("', '", duplicateNames)}'. Runtime contribution names must be unique.");
        }

        ModuleEventSubscriberPipelineContribution[] ambiguousOrderContributions = selectedContributions
            .GroupBy(static contribution => contribution.Order)
            .Where(static group => group.Count() > 1)
            .SelectMany(static group => group)
            .OrderBy(static contribution => contribution.Order)
            .ThenBy(static contribution => contribution.Name, StringComparer.Ordinal)
            .ToArray();

        if (ambiguousOrderContributions.Length == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Module '{module.Name}' has multiple event subscriber runtime pipeline contributions with the same order: '{string.Join("', '", ambiguousOrderContributions.Select(static contribution => contribution.Name))}'. Runtime contribution order must be explicit and unambiguous.");
    }
}
