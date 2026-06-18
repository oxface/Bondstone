using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.ComponentModel;

namespace Bondstone.Configuration;

public static class DurableModulePersistenceServiceCollectionExtensions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static DurableModulePersistenceRegistrationRegistry
        GetOrAddDurableModulePersistenceRegistrationRegistry(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        ServiceDescriptor? descriptor = services.LastOrDefault(
            service => service.ServiceType == typeof(DurableModulePersistenceRegistrationRegistry));

        if (descriptor?.ImplementationInstance is DurableModulePersistenceRegistrationRegistry registry)
        {
            return registry;
        }

        if (descriptor is not null)
        {
            throw new InvalidOperationException(
                "Durable module persistence registration requires DurableModulePersistenceRegistrationRegistry to use the default singleton instance managed by Bondstone.");
        }

        var defaultRegistry = new DurableModulePersistenceRegistrationRegistry();
        services.AddSingleton(defaultRegistry);
        return defaultRegistry;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IServiceCollection UseDurableModuleOutboxDispatchAggregator(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        ServiceDescriptor[] defaultDispatcherDescriptors = services
            .Where(descriptor =>
                descriptor.ServiceType == typeof(IDurableOutboxDispatcher)
                && descriptor.ImplementationType == typeof(DurableOutboxDispatcher))
            .ToArray();

        foreach (ServiceDescriptor descriptor in defaultDispatcherDescriptors)
        {
            services.Remove(descriptor);
        }

        services.TryAddTransient<IDurableOutboxDispatcher, DurableModuleOutboxDispatchAggregator>();
        return services;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IServiceCollection UseDurableModuleIncomingInboxDispatcherAggregator(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        ServiceDescriptor[] defaultDispatcherDescriptors = services
            .Where(descriptor =>
                descriptor.ServiceType == typeof(IDurableIncomingInboxDispatcher)
                && descriptor.ImplementationType == typeof(DurableIncomingInboxDefaultDispatcher))
            .ToArray();

        foreach (ServiceDescriptor descriptor in defaultDispatcherDescriptors)
        {
            services.Remove(descriptor);
        }

        services.TryAddTransient<IDurableIncomingInboxDispatcher, DurableModuleIncomingInboxDispatcherAggregator>();
        return services;
    }
}
