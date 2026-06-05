using Bondstone.Persistence;
using Bondstone.Utility;

namespace Bondstone.Transport.Rebus.Outbox;

public sealed class RebusModuleDestinationResolver : IRebusOutboxDestinationResolver
{
    private readonly IReadOnlyDictionary<string, string> _destinationAddressesByTargetModule;

    public RebusModuleDestinationResolver(
        IReadOnlyDictionary<string, string> destinationAddressesByTargetModule)
    {
        ArgumentNullException.ThrowIfNull(destinationAddressesByTargetModule);

        _destinationAddressesByTargetModule = destinationAddressesByTargetModule
            .Select(static entry => new KeyValuePair<string, string>(
                entry.Key.NormalizeRequired("targetModule", "Target module"),
                entry.Value.NormalizeRequired("destinationAddress", "Rebus destination address")))
            .ToDictionary(
                static entry => entry.Key,
                static entry => entry.Value,
                StringComparer.Ordinal);
    }

    public string ResolveDestinationAddress(DurableOutboxRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        string? targetModule = record.Envelope.TargetModule;
        if (targetModule is null)
        {
            throw new InvalidOperationException(
                "Rebus outbox destination resolution requires a target module.");
        }

        if (_destinationAddressesByTargetModule.TryGetValue(targetModule, out string? destinationAddress))
        {
            return destinationAddress;
        }

        throw new InvalidOperationException(
            $"No Rebus destination address is configured for target module '{targetModule}'.");
    }
}
