using Bondstone.Messaging;
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
        services.TryAddTransient<
            IRebusDurableInboxHandlerExecutor,
            RebusDurableInboxHandlerExecutor>();

        return services;
    }
}
