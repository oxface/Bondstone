using Bondstone.Configuration;

namespace Bondstone.Transport.Rebus.Inbox;

public static class BondstoneRebusModuleCommandReceiveBuilderExtensions
{
    public static BondstoneBuilder UseRebusModuleCommandReceivePipeline(
        this BondstoneBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddBondstoneRebusModuleCommandReceivePipeline();

        return builder;
    }
}

