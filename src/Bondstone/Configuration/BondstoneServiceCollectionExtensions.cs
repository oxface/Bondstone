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
        ModulePipelineContributionRegistry pipelineContributionRegistry =
            GetOrAddOwnedSingleton<ModulePipelineContributionRegistry>(
                services,
                "Module pipeline contribution registration");
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
        services.TryAddScoped<ModuleCommandPipelinePlanner>();
        services.TryAddScoped<ModuleEventSubscriberPipelinePlanner>();
        services.TryAddScoped<IModuleCommandReceivePipeline, ModuleCommandReceivePipeline>();
        services.TryAddScoped<IModuleEventReceivePipeline, ModuleEventReceivePipeline>();
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
        ConfigureDurableOperationReader(services);
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
        pipelineContributionRegistry.AddGlobalCommandContribution(
            new ModuleCommandPipelineContribution(
                "Bondstone.Command.ReceiveInbox",
                ModulePipelineStepKind.System,
                ModuleCommandSystemPipelineOrder.ReceiveInbox,
                typeof(ModuleCommandReceiveInboxPipelineBehavior<>)));
        pipelineContributionRegistry.AddGlobalCommandContribution(
            new ModuleCommandPipelineContribution(
                "Bondstone.Command.OperationState",
                ModulePipelineStepKind.System,
                ModuleCommandSystemPipelineOrder.OperationState,
                typeof(ModuleCommandOperationStatePipelineBehavior<>)));
        pipelineContributionRegistry.AddGlobalCommandContribution(
            new ModuleCommandPipelineContribution(
                "Bondstone.Command.ExecutionContext",
                ModulePipelineStepKind.System,
                ModuleCommandSystemPipelineOrder.ExecutionContext,
                typeof(ModuleExecutionContextPipelineBehavior<>)));
        pipelineContributionRegistry.AddGlobalCommandContribution(
            new ModuleCommandPipelineContribution(
                "Bondstone.Command.Validation",
                ModulePipelineStepKind.System,
                ModuleCommandSystemPipelineOrder.Validation,
                typeof(ValidationModuleCommandPipelineBehavior<>)));
        pipelineContributionRegistry.AddGlobalEventSubscriberContribution(
            new ModuleEventSubscriberPipelineContribution(
                "Bondstone.EventSubscriber.ReceiveInbox",
                ModulePipelineStepKind.System,
                ModuleEventSubscriberSystemPipelineOrder.ReceiveInbox,
                typeof(ModuleEventSubscriberReceiveInboxPipelineBehavior<>)));
        pipelineContributionRegistry.AddGlobalEventSubscriberContribution(
            new ModuleEventSubscriberPipelineContribution(
                "Bondstone.EventSubscriber.ExecutionContext",
                ModulePipelineStepKind.System,
                ModuleEventSubscriberSystemPipelineOrder.ExecutionContext,
                typeof(ModuleEventSubscriberExecutionContextPipelineBehavior<>)));

        var builder = new BondstoneBuilder(
            services,
            messageTypeRegistry,
            commandRouteRegistry,
            publishedEventRegistry,
            eventSubscriberRegistry,
            moduleRegistry,
            pipelineContributionRegistry,
            commandValidatorRegistry);
        builder.AddConfigurationValidator(
            new ModuleRuntimePipelineConfigurationValidator(pipelineContributionRegistry));
        builder.AddConfigurationValidator(
            new DurableModulePersistenceConfigurationValidator(
                persistenceRegistrationRegistry));
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
        bool fallbackAlreadyConfigured = services.Any(static descriptor =>
            descriptor.ServiceType == typeof(DurableOperationReaderFallback));
        ServiceDescriptor? fallbackReaderDescriptor = fallbackAlreadyConfigured
            ? null
            : existingReaderDescriptors.LastOrDefault();

        foreach (ServiceDescriptor descriptor in existingReaderDescriptors)
        {
            services.Remove(descriptor);
        }

        if (!fallbackAlreadyConfigured)
        {
            services.Add(new ServiceDescriptor(
                typeof(DurableOperationReaderFallback),
                serviceProvider => new DurableOperationReaderFallback(
                    () => fallbackReaderDescriptor is null
                        ? null
                        : CreateFallbackOperationReader(
                            serviceProvider,
                            fallbackReaderDescriptor)),
                fallbackReaderDescriptor?.Lifetime ?? ServiceLifetime.Scoped));
        }

        services.AddScoped<IDurableOperationReader>(serviceProvider =>
        {
            DurableOperationReaderFallback? fallback =
                serviceProvider.GetService<DurableOperationReaderFallback>();
            return new DurableModuleOperationReader(
                serviceProvider.GetRequiredService<ModuleRuntimeRegistry>(),
                () => fallback?.Reader
                    ?? serviceProvider.GetService<IDurableOperationStateStore>());
        });
    }

    private static IDurableOperationReader? CreateFallbackOperationReader(
        IServiceProvider serviceProvider,
        ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationInstance is IDurableOperationReader instance)
        {
            return instance;
        }

        if (descriptor.ImplementationFactory is not null)
        {
            return (IDurableOperationReader?)descriptor.ImplementationFactory(serviceProvider);
        }

        if (descriptor.ImplementationType is not null)
        {
            return (IDurableOperationReader)ActivatorUtilities.CreateInstance(
                serviceProvider,
                descriptor.ImplementationType);
        }

        return null;
    }

}
