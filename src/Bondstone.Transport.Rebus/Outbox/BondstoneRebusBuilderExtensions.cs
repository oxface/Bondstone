using Bondstone.Configuration;

namespace Bondstone.Transport.Rebus.Outbox;

public static class BondstoneRebusBuilderExtensions
{
    public static BondstoneOutboxBuilder UseRebusTransport(
        this BondstoneOutboxBuilder outbox,
        IReadOnlyDictionary<string, string> destinationAddressesByTargetModule)
    {
        ArgumentNullException.ThrowIfNull(outbox);

        outbox.Services.AddBondstoneRebusOutboxTransport(destinationAddressesByTargetModule);
        outbox.MarkTransport("Rebus");

        return outbox;
    }
}
