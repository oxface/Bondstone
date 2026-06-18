using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;

namespace Bondstone.Configuration;

/// <summary>
/// Adds Bondstone core services and module registration to an application service collection.
/// </summary>
public static class BondstoneServiceCollectionExtensions
{
    /// <summary>
    /// Registers Bondstone core services, runs module and durable messaging configuration, and validates the resulting setup.
    /// </summary>
    /// <param name="services">The service collection that receives Bondstone registrations.</param>
    /// <param name="configure">Configures modules, durable messaging, persistence, hosting, and transport extensions through the Bondstone builder.</param>
    /// <returns>The same service collection for chained host setup.</returns>
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
        ModuleCommandValidatorRegistry commandValidatorRegistry =
            GetOrAddOwnedSingleton<ModuleCommandValidatorRegistry>(
                services,
                "Module command validator registration");
        DurableModulePersistenceRegistrationRegistry persistenceRegistrationRegistry =
            services.GetOrAddDurableModulePersistenceRegistrationRegistry();
        GetOrAddModuleExecutionContextAccessor(services);
        services.AddBondstoneDurablePayloadSerialization();

        services.TryAddScoped<IModuleCommandExecutor, ModuleCommandExecutor>();
        services.TryAddScoped<IModuleEventSubscriberExecutor, ModuleEventSubscriberExecutor>();
        services.TryAddScoped<IModuleCommandRuntime, ModuleCommandRuntime>();
        services.TryAddScoped<IModuleEventSubscriberRuntime, ModuleEventSubscriberRuntime>();
        services.TryAddScoped<IModuleCommandReceivePipeline, ModuleCommandReceivePipeline>();
        services.TryAddScoped<IModuleEventReceivePipeline, ModuleEventReceivePipeline>();
        services.TryAddScoped<IDurableEnvelopeReceiver, DurableEnvelopeReceiver>();
        services.TryAddSingleton(new DurableIncomingInboxProcessingOptions());
        services.TryAddSingleton<IDurableIncomingInboxFailurePolicy>(serviceProvider =>
            new DurableIncomingInboxFailurePolicy(
                serviceProvider.GetRequiredService<DurableIncomingInboxProcessingOptions>()));
        services.TryAddScoped<IDurableIncomingInboxIngestionBoundaryResolver>(
            serviceProvider =>
                new DurableIncomingInboxIngestionBoundaryResolver(
                    () => CreateFallbackIncomingInboxIngestionBoundary(serviceProvider),
                    serviceProvider.GetRequiredService<ModuleRuntimeRegistry>()));
        services.TryAddSingleton<
            IDurableMessageEnvelopeSerializer,
            SystemTextJsonDurableMessageEnvelopeSerializer>();
        services.TryAddSingleton<DurableOperationResultPayloadSerializer>();
        services.TryAddScoped<IDurableOperationResultReader>(serviceProvider =>
            new DurableOperationResultReader(
                serviceProvider.GetRequiredService<IDurableOperationReader>(),
                serviceProvider.GetRequiredService<DurableOperationResultPayloadSerializer>(),
                serviceProvider.GetService<TimeProvider>()));
        services.TryAddScoped(serviceProvider =>
            new ModuleRuntimeRegistry(
                serviceProvider,
                serviceProvider.GetRequiredService<IBondstoneModuleRegistry>(),
                serviceProvider.GetRequiredService<DurableModulePersistenceRegistrationRegistry>()));
        services.TryAddScoped(serviceProvider =>
            new DurableModuleOutboxWriterResolver(
                () => serviceProvider.GetService<IDurableOutboxWriter>(),
                serviceProvider.GetRequiredService<ModuleRuntimeRegistry>()));
        services.TryAddScoped(serviceProvider =>
            new DurableModuleInboxHandlerExecutorResolver(
                () => serviceProvider.GetService<IDurableInboxHandlerExecutor>(),
                serviceProvider.GetRequiredService<ModuleRuntimeRegistry>()));
        services.TryAddScoped(serviceProvider =>
            new DurableModuleOperationStateStoreResolver(
                () => serviceProvider.GetService<IDurableOperationStateStore>(),
                serviceProvider.GetRequiredService<ModuleRuntimeRegistry>()));
        services.TryAddScoped<IDurableOperationFinalizer>(serviceProvider =>
            new DurableOperationFinalizer(
                serviceProvider.GetRequiredService<ModuleRuntimeRegistry>(),
                serviceProvider.GetService<TimeProvider>()));
        services.TryAddScoped<IDurableOperationExpirationProcessor>(serviceProvider =>
            new DurableOperationExpirationProcessor(
                serviceProvider.GetRequiredService<ModuleRuntimeRegistry>(),
                serviceProvider.GetRequiredService<IDurableOperationFinalizer>()));
        services.TryAddScoped<IDurableOutboxInspector>(serviceProvider =>
            new DurableOutboxInspector(
                serviceProvider.GetRequiredService<ModuleRuntimeRegistry>()));
        services.TryAddScoped<IDurableInboxInspector>(serviceProvider =>
            new DurableInboxInspector(
                serviceProvider.GetRequiredService<ModuleRuntimeRegistry>()));
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
        var builder = new BondstoneBuilder(
            services,
            messageTypeRegistry,
            commandRouteRegistry,
            publishedEventRegistry,
            eventSubscriberRegistry,
            moduleRegistry,
            commandValidatorRegistry);
        builder.AddConfigurationValidator(
            new DurableModulePersistenceConfigurationValidator(
                persistenceRegistrationRegistry));
        configure(builder);
        AddDefaultDurableIncomingInboxDispatcherIfConfigured(services);
        ConfigureDurableOperationReader(services);
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

    private static DurableIncomingInboxIngestionBoundary? CreateFallbackIncomingInboxIngestionBoundary(
        IServiceProvider serviceProvider)
    {
        IDurableIncomingInboxIngestionStore? store =
            serviceProvider.GetService<IDurableIncomingInboxIngestionStore>();
        IDurableIncomingInboxIngestionPersistenceScope? persistenceScope =
            serviceProvider.GetService<IDurableIncomingInboxIngestionPersistenceScope>();

        return store is null || persistenceScope is null
            ? null
            : new DurableIncomingInboxIngestionBoundary(store, persistenceScope);
    }

    private static void AddDefaultDurableIncomingInboxDispatcherIfConfigured(
        IServiceCollection services)
    {
        bool hasDispatcher = services.Any(static service =>
            service.ServiceType == typeof(IDurableIncomingInboxDispatcher));
        if (hasDispatcher)
        {
            return;
        }

        bool hasIncomingInboxProcessingStores = services.Any(static service =>
                service.ServiceType == typeof(IDurableIncomingInboxClaimer))
            && services.Any(static service =>
                service.ServiceType == typeof(IDurableIncomingInboxOutcomeRecorder));
        if (!hasIncomingInboxProcessingStores)
        {
            return;
        }

        services.TryAddScoped<IDurableIncomingInboxDispatcher, DurableIncomingInboxDefaultDispatcher>();
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

    private static TImplementation GetOrAddOwnedSingleton<TImplementation>(
        IServiceCollection services,
        string ownerDescription)
        where TImplementation : class, new()
    {
        ServiceDescriptor? descriptor = services.LastOrDefault(
            service => service.ServiceType == typeof(TImplementation));

        if (descriptor?.ImplementationInstance is TImplementation implementation)
        {
            return implementation;
        }

        if (descriptor is not null)
        {
            throw new InvalidOperationException(
                $"{ownerDescription} requires {typeof(TImplementation).Name} to use the default singleton instance managed by {nameof(AddBondstone)}.");
        }

        var defaultImplementation = new TImplementation();
        services.AddSingleton(defaultImplementation);
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

    private static void ConfigureDurableOperationReader(IServiceCollection services)
    {
        ServiceDescriptor[] existingReaderDescriptors = services
            .Where(static descriptor => descriptor.ServiceType == typeof(IDurableOperationReader))
            .ToArray();

        foreach (ServiceDescriptor descriptor in existingReaderDescriptors)
        {
            services.Remove(descriptor);
        }

        services.AddScoped<IDurableOperationReader>(serviceProvider =>
            new DurableModuleOperationReader(
                serviceProvider.GetRequiredService<ModuleRuntimeRegistry>()));
    }

}
