using Bondstone.Transport.Rebus.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rebus.Handlers;

namespace Bondstone.Transport.Rebus.Inbox;

public static class BondstoneRebusModuleCommandEndpointServiceCollectionExtensions
{
    public static IServiceCollection AddBondstoneRebusModuleCommandEndpointHandler(
        this IServiceCollection services,
        string endpointName)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new RebusModuleCommandEndpointHandlerOptions(endpointName);
        EnsureEndpointBindingIsConsistent(services, options);

        services.TryAddSingleton(options);
        services.TryAddTransient<RebusModuleCommandEndpointHandler>();
        services.TryAddEnumerable(
            ServiceDescriptor.Transient<
                IHandleMessages<RebusDurableMessageEnvelope>,
                RebusModuleCommandEndpointHandler>());

        return services;
    }

    private static void EnsureEndpointBindingIsConsistent(
        IServiceCollection services,
        RebusModuleCommandEndpointHandlerOptions options)
    {
        ServiceDescriptor? existingDescriptor = services.FirstOrDefault(
            static descriptor =>
                descriptor.ServiceType == typeof(RebusModuleCommandEndpointHandlerOptions));

        if (existingDescriptor?.ImplementationInstance
                is not RebusModuleCommandEndpointHandlerOptions existingOptions)
        {
            return;
        }

        if (existingOptions.EndpointName == options.EndpointName)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Rebus module command endpoint handler is already bound to endpoint '{existingOptions.EndpointName}'.");
    }
}
