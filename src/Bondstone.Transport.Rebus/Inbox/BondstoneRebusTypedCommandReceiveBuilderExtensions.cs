using Bondstone.Configuration;

namespace Bondstone.Transport.Rebus.Inbox;

public static class BondstoneRebusTypedCommandReceiveBuilderExtensions
{
    public static BondstoneBuilder UseRebusTypedCommandReceivePipeline(
        this BondstoneBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddBondstoneRebusTypedCommandReceivePipeline();

        return builder;
    }
}
