using Bondstone.Utility;

namespace Bondstone.Transport.Rebus.Outbox;

public sealed class BondstoneRebusTransportBuilder
{
    private readonly Dictionary<string, string> _destinationAddressesByTargetModule =
        new(StringComparer.Ordinal);

    internal IReadOnlyDictionary<string, string> DestinationAddressesByTargetModule =>
        _destinationAddressesByTargetModule;

    public BondstoneRebusModuleRouteBuilder RouteModule(string targetModule)
    {
        string normalizedTargetModule = targetModule.NormalizeRequired(
            nameof(targetModule),
            "Target module");

        return new BondstoneRebusModuleRouteBuilder(this, normalizedTargetModule);
    }

    internal void SetModuleDestinationAddress(
        string targetModule,
        string destinationAddress)
    {
        string normalizedDestinationAddress = destinationAddress.NormalizeRequired(
            nameof(destinationAddress),
            "Rebus destination address");

        if (_destinationAddressesByTargetModule.TryGetValue(
            targetModule,
            out string? existingDestinationAddress))
        {
            if (existingDestinationAddress == normalizedDestinationAddress)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Target module '{targetModule}' already routes to Rebus destination address '{existingDestinationAddress}'.");
        }

        _destinationAddressesByTargetModule.Add(
            targetModule,
            normalizedDestinationAddress);
    }
}
