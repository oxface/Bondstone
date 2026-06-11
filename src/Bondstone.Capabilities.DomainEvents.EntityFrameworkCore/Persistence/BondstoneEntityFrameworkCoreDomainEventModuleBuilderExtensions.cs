using Bondstone.Capabilities.DomainEvents.EntityFrameworkCore.DomainEvents;
using Bondstone.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bondstone.Capabilities.DomainEvents.EntityFrameworkCore.Persistence;

public static class BondstoneEntityFrameworkCoreDomainEventModuleBuilderExtensions
{
    public static BondstoneModuleBuilder UseEntityFrameworkCoreDomainEventPersistence(
        this BondstoneModuleBuilder module)
    {
        ArgumentNullException.ThrowIfNull(module);

        module.Services.AddSingleton(new EntityFrameworkCoreDomainEventPersistenceModule(module.Name));
        module.Services.TryAddEntityFrameworkCoreDomainEventSystemBehaviors();

        return module;
    }

    private static void TryAddEntityFrameworkCoreDomainEventSystemBehaviors(
        this IServiceCollection services)
    {
        services.TryAddScoped(serviceProvider =>
            new EntityFrameworkCoreDomainEventModuleRuntimeRegistry(
                serviceProvider.GetRequiredService<IBondstoneModuleRegistry>(),
                serviceProvider.GetServices<EntityFrameworkCoreDomainEventPersistenceModule>()));
        services.TryAddEnumerable(ServiceDescriptor.Scoped(
            typeof(IModuleCommandSystemPipelineBehavior<>),
            typeof(EntityFrameworkCoreDomainEventModuleCommandBehavior<>)));
        services.TryAddEnumerable(ServiceDescriptor.Scoped(
            typeof(IModuleEventSubscriberSystemPipelineBehavior<>),
            typeof(EntityFrameworkCoreDomainEventModuleEventSubscriberBehavior<>)));
    }
}
