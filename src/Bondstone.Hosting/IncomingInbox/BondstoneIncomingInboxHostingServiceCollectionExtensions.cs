using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Bondstone.Hosting.IncomingInbox;

public static class BondstoneIncomingInboxHostingServiceCollectionExtensions
{
    public static IServiceCollection AddBondstoneDurableIncomingInboxWorker(
        this IServiceCollection services,
        Action<DurableIncomingInboxWorkerOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<DurableIncomingInboxWorkerOptions>();

        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IValidateOptions<DurableIncomingInboxWorkerOptions>,
                DurableIncomingInboxWorkerOptionsValidator>());
        services.Replace(
            ServiceDescriptor.Singleton(serviceProvider =>
                serviceProvider.GetRequiredService<IOptions<DurableIncomingInboxWorkerOptions>>()
                    .Value
                    .CreateProcessingOptions()));
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostedService, DurableIncomingInboxWorker>());

        return services;
    }
}
