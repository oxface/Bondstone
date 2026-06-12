using Bondstone.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Bondstone.Modules;

public sealed class ModuleCommandPipelineContribution
{
    private readonly Func<BondstoneModuleRegistration, bool> _appliesToModule;
    private readonly Func<IServiceProvider, Type, object> _createBehavior;

    public ModuleCommandPipelineContribution(
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
            (serviceProvider, commandType) => CreateBehavior(
                serviceProvider,
                behaviorType,
                commandType))
    {
        ValidateBehaviorType(behaviorType);
    }

    public ModuleCommandPipelineContribution(
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

    internal IModuleCommandPipelineBehavior<TCommand> CreateBehavior<TCommand>(
        IServiceProvider serviceProvider)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        object behavior = _createBehavior(serviceProvider, typeof(TCommand))
            ?? throw new InvalidOperationException(
                $"Command pipeline contribution '{Name}' returned null.");

        return behavior as IModuleCommandPipelineBehavior<TCommand>
            ?? throw new InvalidOperationException(
                $"Command pipeline contribution '{Name}' created behavior type "
                + $"'{behavior.GetType().FullName}', which does not implement "
                + $"'{typeof(IModuleCommandPipelineBehavior<TCommand>).FullName}'.");
    }

    private static object CreateBehavior(
        IServiceProvider serviceProvider,
        Type behaviorType,
        Type commandType)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(behaviorType);
        ArgumentNullException.ThrowIfNull(commandType);

        Type implementationType = behaviorType.IsGenericTypeDefinition
            ? behaviorType.MakeGenericType(commandType)
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
                $"Command pipeline contribution behavior type '{behaviorType.FullName}' must be a concrete implementation type.",
                nameof(behaviorType));
        }

        Type expectedInterface = typeof(IModuleCommandPipelineBehavior<>);
        bool implementsExpectedInterface = behaviorType
            .GetInterfaces()
            .Any(type => type.IsGenericType
                && type.GetGenericTypeDefinition() == expectedInterface);
        if (!implementsExpectedInterface)
        {
            throw new ArgumentException(
                $"Command pipeline contribution behavior type '{behaviorType.FullName}' must implement '{expectedInterface.FullName}'.",
                nameof(behaviorType));
        }
    }
}
