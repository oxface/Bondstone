using System.Text.Json;
using Bondstone.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bondstone.Configuration;

public static class BondstoneDurablePayloadServiceCollectionExtensions
{
    public static IServiceCollection AddBondstoneDurablePayloadSerialization(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<DurablePayloadJsonOptions>();
        services.TryAddSingleton<
            IDurablePayloadSerializer,
            SystemTextJsonDurablePayloadSerializer>();

        return services;
    }

    public static IServiceCollection ConfigureBondstoneDurablePayloadJson(
        this IServiceCollection services,
        Action<JsonSerializerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddBondstoneDurablePayloadSerialization();

        var options = new DurablePayloadJsonOptions();
        configure(options.JsonSerializerOptions);
        services.Replace(ServiceDescriptor.Singleton(options));

        return services;
    }
}
