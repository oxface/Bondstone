using Bondstone.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bondstone.Capabilities.DomainEvents;

public static class BondstoneDomainEventModuleBuilderExtensions
{
    public static BondstoneModuleBuilder UseDomainEventDispatch(
        this BondstoneModuleBuilder module)
    {
        ArgumentNullException.ThrowIfNull(module);

        module.Services.AddSingleton(new DomainEventDispatchModule(module.Name));
        module.Services.TryAddDomainEventSystemBehaviors();

        return module;
    }

    private static void TryAddDomainEventSystemBehaviors(
        this IServiceCollection services)
    {
        services.TryAddScoped(serviceProvider =>
            new DomainEventDispatchModuleRuntimeRegistry(
                serviceProvider.GetRequiredService<IBondstoneModuleRegistry>(),
                serviceProvider.GetServices<DomainEventDispatchModule>()));
        services.TryAddEnumerable(ServiceDescriptor.Scoped(
            typeof(IModuleCommandSystemPipelineBehavior<>),
            typeof(DomainEventModuleCommandBehavior<>)));
        services.TryAddEnumerable(ServiceDescriptor.Scoped(
            typeof(IModuleEventSubscriberSystemPipelineBehavior<>),
            typeof(DomainEventModuleEventSubscriberBehavior<>)));
    }
}
