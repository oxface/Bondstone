using Bondstone.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;

namespace Bondstone.Modules;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ModuleEventSubscriberPipelineContribution
{
    private readonly Func<BondstoneModuleRegistration, bool> _appliesToModule;
    private readonly Func<IServiceProvider, Type, object> _createBehavior;
    private Type? _behaviorType;

    public ModuleEventSubscriberPipelineContribution(
        string name,
        ModulePipelineStepKind kind,
        int order,
        Type behaviorType,
        Func<BondstoneModuleRegistration, bool>? appliesToModule = null)
        : this(
            name,
            kind,
            order,
            appliesToModule,
            (serviceProvider, eventType) => CreateBehavior(
                serviceProvider,
                behaviorType,
                eventType))
    {
        ValidateBehaviorType(behaviorType);
        _behaviorType = behaviorType;
    }

    public ModuleEventSubscriberPipelineContribution(
        string name,
        ModulePipelineStepKind kind,
        int order,
        Func<BondstoneModuleRegistration, bool>? appliesToModule,
        Func<IServiceProvider, Type, object> createBehavior)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Pipeline contribution name is required.", nameof(name));
        }

        if (kind == ModulePipelineStepKind.Application)
        {
            throw new ArgumentException(
                "Application pipeline behavior remains ordinary DI registration.",
                nameof(kind));
        }

        Name = name.Trim();
        Kind = kind;
        Order = order;
        _appliesToModule = appliesToModule ?? (_ => true);
        _createBehavior = createBehavior ?? throw new ArgumentNullException(nameof(createBehavior));
    }

    public string Name { get; }

    public ModulePipelineStepKind Kind { get; }

    public int Order { get; }

    public bool AppliesTo(BondstoneModuleRegistration module)
    {
        ArgumentNullException.ThrowIfNull(module);

        return _appliesToModule(module);
    }

    internal bool IsEquivalentTo(ModuleEventSubscriberPipelineContribution other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return StringComparer.Ordinal.Equals(Name, other.Name)
            && Kind == other.Kind
            && Order == other.Order
            && _behaviorType is not null
            && _behaviorType == other._behaviorType;
    }

    internal IModuleEventSubscriberPipelineBehavior<TEvent> CreateBehavior<TEvent>(
        IServiceProvider serviceProvider)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        object behavior = _createBehavior(serviceProvider, typeof(TEvent))
            ?? throw new InvalidOperationException(
                $"Event subscriber pipeline contribution '{Name}' returned null.");

        return behavior as IModuleEventSubscriberPipelineBehavior<TEvent>
            ?? throw new InvalidOperationException(
                $"Event subscriber pipeline contribution '{Name}' created behavior type "
                + $"'{behavior.GetType().FullName}', which does not implement "
                + $"'{typeof(IModuleEventSubscriberPipelineBehavior<TEvent>).FullName}'.");
    }

    private static object CreateBehavior(
        IServiceProvider serviceProvider,
        Type behaviorType,
        Type eventType)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(behaviorType);
        ArgumentNullException.ThrowIfNull(eventType);

        Type implementationType = behaviorType.IsGenericTypeDefinition
            ? behaviorType.MakeGenericType(eventType)
            : behaviorType;

        return ActivatorUtilities.CreateInstance(
            serviceProvider,
            implementationType);
    }

    private static void ValidateBehaviorType(Type behaviorType)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);

        if (behaviorType.IsInterface || behaviorType.IsAbstract)
        {
            throw new ArgumentException(
                $"Event subscriber pipeline contribution behavior type '{behaviorType.FullName}' must be a concrete implementation type.",
                nameof(behaviorType));
        }

        Type expectedInterface = typeof(IModuleEventSubscriberPipelineBehavior<>);
        bool implementsExpectedInterface = behaviorType
            .GetInterfaces()
            .Any(type => type.IsGenericType
                && type.GetGenericTypeDefinition() == expectedInterface);
        if (!implementsExpectedInterface)
        {
            throw new ArgumentException(
                $"Event subscriber pipeline contribution behavior type '{behaviorType.FullName}' must implement '{expectedInterface.FullName}'.",
                nameof(behaviorType));
        }
    }
}
