using Bondstone.Utility;

namespace Bondstone.Modules;

internal sealed class ModulePipelineContributionRegistry
{
    private readonly object _syncRoot = new();
    private readonly List<ModuleCommandPipelineContribution> _globalCommandContributions = [];
    private readonly List<ModuleEventSubscriberPipelineContribution> _globalEventSubscriberContributions = [];
    private readonly Dictionary<string, List<ModuleCommandPipelineContribution>>
        _moduleCommandContributions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<ModuleEventSubscriberPipelineContribution>>
        _moduleEventSubscriberContributions = new(StringComparer.Ordinal);

    public void AddGlobalCommandContribution(ModuleCommandPipelineContribution contribution)
    {
        ArgumentNullException.ThrowIfNull(contribution);

        lock (_syncRoot)
        {
            AddContribution(_globalCommandContributions, contribution);
        }
    }

    public void AddGlobalEventSubscriberContribution(
        ModuleEventSubscriberPipelineContribution contribution)
    {
        ArgumentNullException.ThrowIfNull(contribution);

        lock (_syncRoot)
        {
            AddContribution(_globalEventSubscriberContributions, contribution);
        }
    }

    public void AddModuleCommandContribution(
        string moduleName,
        ModuleCommandPipelineContribution contribution)
    {
        ArgumentNullException.ThrowIfNull(contribution);

        string normalizedModuleName = NormalizeModuleName(moduleName);

        lock (_syncRoot)
        {
            if (!_moduleCommandContributions.TryGetValue(
                    normalizedModuleName,
                    out List<ModuleCommandPipelineContribution>? contributions))
            {
                contributions = [];
                _moduleCommandContributions.Add(normalizedModuleName, contributions);
            }

            AddContribution(contributions, contribution);
        }
    }

    public void AddModuleEventSubscriberContribution(
        string moduleName,
        ModuleEventSubscriberPipelineContribution contribution)
    {
        ArgumentNullException.ThrowIfNull(contribution);

        string normalizedModuleName = NormalizeModuleName(moduleName);

        lock (_syncRoot)
        {
            if (!_moduleEventSubscriberContributions.TryGetValue(
                    normalizedModuleName,
                    out List<ModuleEventSubscriberPipelineContribution>? contributions))
            {
                contributions = [];
                _moduleEventSubscriberContributions.Add(normalizedModuleName, contributions);
            }

            AddContribution(contributions, contribution);
        }
    }

    public IReadOnlyList<ModuleCommandPipelineContribution> GetCommandContributions(
        BondstoneModuleRegistration module)
    {
        ArgumentNullException.ThrowIfNull(module);

        lock (_syncRoot)
        {
            return GetContributions(
                _globalCommandContributions,
                _moduleCommandContributions,
                module);
        }
    }

    public IReadOnlyList<ModuleEventSubscriberPipelineContribution> GetEventSubscriberContributions(
        BondstoneModuleRegistration module)
    {
        ArgumentNullException.ThrowIfNull(module);

        lock (_syncRoot)
        {
            return GetContributions(
                _globalEventSubscriberContributions,
                _moduleEventSubscriberContributions,
                module);
        }
    }

    private static IReadOnlyList<TContribution> GetContributions<TContribution>(
        IReadOnlyCollection<TContribution> globalContributions,
        IReadOnlyDictionary<string, List<TContribution>> moduleContributions,
        BondstoneModuleRegistration module)
        where TContribution : class
    {
        string moduleName = NormalizeModuleName(module.Name);
        if (!moduleContributions.TryGetValue(
                moduleName,
                out List<TContribution>? moduleSpecificContributions))
        {
            return globalContributions.ToArray();
        }

        return globalContributions
            .Concat(moduleSpecificContributions)
            .ToArray();
    }

    private static void AddContribution<TContribution>(
        List<TContribution> contributions,
        TContribution contribution)
        where TContribution : class
    {
        string contributionName = contribution switch
        {
            ModuleCommandPipelineContribution commandContribution => commandContribution.Name,
            ModuleEventSubscriberPipelineContribution eventSubscriberContribution =>
                eventSubscriberContribution.Name,
            _ => throw new ArgumentException(
                $"Unsupported pipeline contribution type '{typeof(TContribution).FullName}'.",
                nameof(contribution)),
        };

        TContribution? existingContribution = contributions.FirstOrDefault(existing =>
            StringComparer.Ordinal.Equals(
                GetContributionName(existing),
                contributionName));
        if (existingContribution is null)
        {
            contributions.Add(contribution);
            return;
        }

        (ModulePipelineStepKind existingKind, int existingOrder) =
            GetContributionSlot(existingContribution);
        (ModulePipelineStepKind contributionKind, int contributionOrder) =
            GetContributionSlot(contribution);
        if (existingKind == contributionKind && existingOrder == contributionOrder)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Pipeline contribution '{contributionName}' is already registered with kind "
            + $"'{existingKind}' and order '{existingOrder}', and cannot also use kind "
            + $"'{contributionKind}' and order '{contributionOrder}'.");
    }

    private static string GetContributionName<TContribution>(TContribution contribution)
        where TContribution : class
    {
        return contribution switch
        {
            ModuleCommandPipelineContribution commandContribution => commandContribution.Name,
            ModuleEventSubscriberPipelineContribution eventSubscriberContribution =>
                eventSubscriberContribution.Name,
            _ => throw new ArgumentException(
                $"Unsupported pipeline contribution type '{typeof(TContribution).FullName}'.",
                nameof(contribution)),
        };
    }

    private static (ModulePipelineStepKind Kind, int Order) GetContributionSlot<TContribution>(
        TContribution contribution)
        where TContribution : class
    {
        return contribution switch
        {
            ModuleCommandPipelineContribution commandContribution =>
                (commandContribution.Kind, commandContribution.Order),
            ModuleEventSubscriberPipelineContribution eventSubscriberContribution =>
                (eventSubscriberContribution.Kind, eventSubscriberContribution.Order),
            _ => throw new ArgumentException(
                $"Unsupported pipeline contribution type '{typeof(TContribution).FullName}'.",
                nameof(contribution)),
        };
    }

    private static string NormalizeModuleName(string moduleName)
    {
        return moduleName.NormalizeRequired(nameof(moduleName), "Module name");
    }
}
