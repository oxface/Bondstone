using Bondstone.Persistence;
using Bondstone.Utility;

namespace Bondstone.Transport.Rebus.Outbox;

public sealed class RebusModuleDestinationResolver : IRebusOutboxDestinationResolver
{
    private readonly IReadOnlyDictionary<string, string> _destinationAddressesByTargetModule;
    private readonly Func<string, string>? _destinationAddressConvention;

    public RebusModuleDestinationResolver(
        IReadOnlyDictionary<string, string> destinationAddressesByTargetModule,
        Func<string, string>? destinationAddressConvention = null)
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
        _destinationAddressConvention = destinationAddressConvention;
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

        string normalizedTargetModule = targetModule.NormalizeRequired(
            nameof(targetModule),
            "Target module");

        if (_destinationAddressesByTargetModule.TryGetValue(
            normalizedTargetModule,
            out string? destinationAddress))
        {
            return destinationAddress;
        }

        if (_destinationAddressConvention is not null)
        {
            return _destinationAddressConvention(normalizedTargetModule).NormalizeRequired(
                nameof(_destinationAddressConvention),
                "Rebus destination address");
        }

        throw new InvalidOperationException(
            $"No Rebus destination address is configured for target module '{normalizedTargetModule}'.");
    }
}
