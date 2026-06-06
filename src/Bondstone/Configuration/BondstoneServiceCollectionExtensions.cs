using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Bondstone.Messaging;
using Bondstone.Modules;

namespace Bondstone.Configuration;

public static class BondstoneServiceCollectionExtensions
{
    public static IServiceCollection AddBondstone(
        this IServiceCollection services,
        Action<BondstoneBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        MessageTypeRegistry messageTypeRegistry = GetOrAddMessageTypeRegistry(services);
        ModuleCommandRouteRegistry commandRouteRegistry = GetOrAddCommandRouteRegistry(services);

        services.TryAddScoped<IModuleCommandExecutor, ModuleCommandExecutor>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped(
            typeof(IModuleCommandPipelineBehavior<>),
            typeof(ValidationModuleCommandPipelineBehavior<>)));

        var builder = new BondstoneBuilder(
            services,
            messageTypeRegistry,
            commandRouteRegistry);
        configure(builder);
        builder.Validate();

        return services;
    }

    private static MessageTypeRegistry GetOrAddMessageTypeRegistry(IServiceCollection services)
    {
        ServiceDescriptor? descriptor = services.LastOrDefault(
            service => service.ServiceType == typeof(IMessageTypeRegistry));

        if (descriptor?.ImplementationInstance is MessageTypeRegistry messageTypeRegistry)
        {
            return messageTypeRegistry;
        }

        if (descriptor is { ImplementationType: not null, Lifetime: ServiceLifetime.Singleton }
            && descriptor.ImplementationType == typeof(MessageTypeRegistry))
        {
            services.Remove(descriptor);
            var defaultRegistry = new MessageTypeRegistry();
            services.AddSingleton<IMessageTypeRegistry>(defaultRegistry);
            return defaultRegistry;
        }

        if (descriptor is not null)
        {
            throw new InvalidOperationException(
                $"Module command registration requires {nameof(IMessageTypeRegistry)} to be registered as a {nameof(MessageTypeRegistry)} singleton instance before {nameof(AddBondstone)} when overriding the default registry.");
        }

        var registry = new MessageTypeRegistry();
        services.AddSingleton<IMessageTypeRegistry>(registry);
        return registry;
    }

    private static ModuleCommandRouteRegistry GetOrAddCommandRouteRegistry(IServiceCollection services)
    {
        ServiceDescriptor? descriptor = services.LastOrDefault(
            service => service.ServiceType == typeof(IModuleCommandRouteRegistry));

        if (descriptor?.ImplementationInstance is ModuleCommandRouteRegistry commandRouteRegistry)
        {
            return commandRouteRegistry;
        }

        if (descriptor is not null)
        {
            throw new InvalidOperationException(
                $"Module command registration requires {nameof(IModuleCommandRouteRegistry)} to use the default singleton instance managed by {nameof(AddBondstone)}.");
        }

        var registry = new ModuleCommandRouteRegistry();
        services.AddSingleton<IModuleCommandRouteRegistry>(registry);
        return registry;
    }
}
