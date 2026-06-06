using Bondstone.Configuration;

namespace Bondstone.Transport.Rebus.Inbox;

public static class BondstoneRebusInboxBuilderExtensions
{
    public static BondstoneBuilder UseRebusInbox(
        this BondstoneBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddBondstoneRebusInbox();

        return builder;
    }
}
