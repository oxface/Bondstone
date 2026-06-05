using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Bondstone.Hosting.Outbox;

public static class BondstoneHostingServiceCollectionExtensions
{
    public static IServiceCollection AddBondstoneDurableOutboxDispatcher(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddTransient<IDurableOutboxDispatcher, DurableOutboxDispatcher>();
        services.TryAddSingleton<IDurableOutboxFailurePolicy, DurableOutboxFailurePolicy>();

        return services;
    }

    public static IServiceCollection AddBondstoneDurableOutboxWorker(
        this IServiceCollection services,
        Action<DurableOutboxWorkerOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        services.AddBondstoneDurableOutboxDispatcher();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IValidateOptions<DurableOutboxWorkerOptions>,
                DurableOutboxWorkerOptionsValidator>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostedService, DurableOutboxWorker>());

        return services;
    }
}
