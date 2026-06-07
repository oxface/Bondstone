using Bondstone.Messaging;
using Bondstone.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bondstone.Transport.Rebus.Inbox;

public static class BondstoneRebusInboxServiceCollectionExtensions
{
    public static IServiceCollection AddBondstoneRebusInbox(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IMessageTypeRegistry, MessageTypeRegistry>();
        services.AddBondstoneDurablePayloadSerialization();
        services.TryAddTransient<
            IRebusDurableInboxHandlerExecutor,
            RebusDurableInboxHandlerExecutor>();

        return services;
    }
}
