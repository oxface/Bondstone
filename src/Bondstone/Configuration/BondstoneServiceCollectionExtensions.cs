using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;

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
        ModuleCommandRouteRegistry commandRouteRegistry =
            GetOrAddOwnedSingleton<IModuleCommandRouteRegistry, ModuleCommandRouteRegistry>(
                services,
                "Module command registration");
        ModulePublishedEventRegistry publishedEventRegistry =
            GetOrAddOwnedSingleton<IModulePublishedEventRegistry, ModulePublishedEventRegistry>(
                services,
                "Module published event registration");
        ModuleEventSubscriberRegistry eventSubscriberRegistry =
            GetOrAddOwnedSingleton<IModuleEventSubscriberRegistry, ModuleEventSubscriberRegistry>(
                services,
                "Module event subscriber registration");
        BondstoneModuleRegistry moduleRegistry =
            GetOrAddOwnedSingleton<IBondstoneModuleRegistry, BondstoneModuleRegistry>(
                services,
                "Module registration");
        GetOrAddModuleExecutionContextAccessor(services);
        services.AddBondstoneDurablePayloadSerialization();

        services.TryAddScoped<IModuleCommandExecutor, ModuleCommandExecutor>();
        services.TryAddScoped<IModuleEventSubscriberExecutor, ModuleEventSubscriberExecutor>();
        services.TryAddScoped<ModuleCommandPipelinePlanner>();
        services.TryAddScoped<ModuleEventSubscriberPipelinePlanner>();
        services.TryAddScoped<IModuleCommandReceivePipeline, ModuleCommandReceivePipeline>();
        services.TryAddScoped<IModuleEventReceivePipeline, ModuleEventReceivePipeline>();
        services.TryAddScoped(serviceProvider =>
            new ModuleRuntimeRegistry(
                serviceProvider.GetRequiredService<IBondstoneModuleRegistry>(),
                serviceProvider.GetServices<IDurableModuleOutboxWriter>(),
                serviceProvider.GetServices<IDurableModuleInboxHandlerExecutor>(),
                serviceProvider.GetServices<IDurableModuleOperationStateStore>()));
        services.TryAddScoped(serviceProvider =>
            new DurableModuleOutboxWriterResolver(
                serviceProvider.GetService<IDurableOutboxWriter>(),
                serviceProvider.GetRequiredService<ModuleRuntimeRegistry>()));
        services.TryAddScoped(serviceProvider =>
            new DurableModuleInboxHandlerExecutorResolver(
                serviceProvider.GetService<IDurableInboxHandlerExecutor>(),
                serviceProvider.GetRequiredService<ModuleRuntimeRegistry>()));
        services.TryAddScoped(serviceProvider =>
            new DurableModuleOperationStateStoreResolver(
                serviceProvider.GetService<IDurableOperationStateStore>(),
                serviceProvider.GetRequiredService<ModuleRuntimeRegistry>()));
        services.TryAddScoped<IDurableOperationReader>(serviceProvider =>
            new DurableModuleOperationReader(
                serviceProvider.GetRequiredService<ModuleRuntimeRegistry>(),
                serviceProvider.GetService<IDurableOperationStateStore>()));
        services.TryAddScoped<IDurableCommandSender>(serviceProvider =>
            new DurableCommandSender(
                serviceProvider.GetRequiredService<DurableModuleOutboxWriterResolver>(),
                serviceProvider.GetRequiredService<IMessageTypeRegistry>(),
                serviceProvider.GetRequiredService<IModuleExecutionContextAccessor>(),
                serviceProvider.GetRequiredService<DurableModuleOperationStateStoreResolver>(),
                serviceProvider.GetRequiredService<IDurablePayloadSerializer>(),
                serviceProvider.GetService<TimeProvider>()));
        services.TryAddScoped<IDurableEventPublisher>(serviceProvider =>
            new DurableEventPublisher(
                serviceProvider.GetRequiredService<DurableModuleOutboxWriterResolver>(),
                serviceProvider.GetRequiredService<IMessageTypeRegistry>(),
                serviceProvider.GetRequiredService<IModulePublishedEventRegistry>(),
                serviceProvider.GetRequiredService<IModuleExecutionContextAccessor>(),
                serviceProvider.GetRequiredService<IDurablePayloadSerializer>(),
                serviceProvider.GetService<TimeProvider>()));
        services.TryAddEnumerable(ServiceDescriptor.Scoped(
            typeof(IModuleCommandSystemPipelineBehavior<>),
            typeof(ModuleCommandReceiveInboxPipelineBehavior<>)));
        services.TryAddEnumerable(ServiceDescriptor.Scoped(
            typeof(IModuleCommandSystemPipelineBehavior<>),
            typeof(ModuleCommandOperationStatePipelineBehavior<>)));
        services.TryAddEnumerable(ServiceDescriptor.Scoped(
            typeof(IModuleCommandSystemPipelineBehavior<>),
            typeof(ModuleExecutionContextPipelineBehavior<>)));
        services.TryAddEnumerable(ServiceDescriptor.Scoped(
            typeof(IModuleCommandPipelineBehavior<>),
            typeof(ValidationModuleCommandPipelineBehavior<>)));
        services.TryAddEnumerable(ServiceDescriptor.Scoped(
            typeof(IModuleEventSubscriberSystemPipelineBehavior<>),
            typeof(ModuleEventSubscriberReceiveInboxPipelineBehavior<>)));
        services.TryAddEnumerable(ServiceDescriptor.Scoped(
            typeof(IModuleEventSubscriberSystemPipelineBehavior<>),
            typeof(ModuleEventSubscriberExecutionContextPipelineBehavior<>)));

        var builder = new BondstoneBuilder(
            services,
            messageTypeRegistry,
            commandRouteRegistry,
            publishedEventRegistry,
            eventSubscriberRegistry,
            moduleRegistry);
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

    private static TImplementation GetOrAddOwnedSingleton<TService, TImplementation>(
        IServiceCollection services,
        string ownerDescription)
        where TService : class
        where TImplementation : class, TService, new()
    {
        ServiceDescriptor? descriptor = services.LastOrDefault(
            service => service.ServiceType == typeof(TService));

        if (descriptor?.ImplementationInstance is TImplementation implementation)
        {
            return implementation;
        }

        if (descriptor is not null)
        {
            throw new InvalidOperationException(
                $"{ownerDescription} requires {typeof(TService).Name} to use the default singleton instance managed by {nameof(AddBondstone)}.");
        }

        var defaultImplementation = new TImplementation();
        services.AddSingleton<TService>(defaultImplementation);
        return defaultImplementation;
    }

    private static ModuleExecutionContextAccessor GetOrAddModuleExecutionContextAccessor(
        IServiceCollection services)
    {
        ServiceDescriptor? concreteDescriptor = services.LastOrDefault(
            service => service.ServiceType == typeof(ModuleExecutionContextAccessor));

        if (concreteDescriptor?.ImplementationInstance is ModuleExecutionContextAccessor accessor)
        {
            return accessor;
        }

        if (concreteDescriptor is not null)
        {
            throw new InvalidOperationException(
                $"Module execution context registration requires {nameof(ModuleExecutionContextAccessor)} to use the default singleton instance managed by {nameof(AddBondstone)}.");
        }

        ServiceDescriptor? publicDescriptor = services.LastOrDefault(
            service => service.ServiceType == typeof(IModuleExecutionContextAccessor));
        if (publicDescriptor is not null)
        {
            throw new InvalidOperationException(
                $"Module execution context registration requires {nameof(IModuleExecutionContextAccessor)} to use the default singleton instance managed by {nameof(AddBondstone)}.");
        }

        var defaultAccessor = new ModuleExecutionContextAccessor();
        services.AddSingleton(defaultAccessor);
        services.AddSingleton<IModuleExecutionContextAccessor>(defaultAccessor);
        return defaultAccessor;
    }

}
