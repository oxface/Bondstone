using Bondstone.Persistence;
using Bondstone.Utility;

namespace Bondstone.Transport.Rebus.Outbox;

public sealed class RebusModuleDestinationResolver : IRebusOutboxDestinationResolver
{
    private readonly RebusCommandDestinationTopology _topology;

    public RebusModuleDestinationResolver(
        IReadOnlyDictionary<string, string> destinationAddressesByTargetModule,
        Func<string, string>? destinationAddressConvention = null)
        : this(RebusCommandDestinationTopology.FromConfiguredDestinations(
            destinationAddressesByTargetModule,
            destinationAddressConvention))
    {
    }

    internal RebusModuleDestinationResolver(
        RebusCommandDestinationTopology topology)
    {
        _topology = topology ?? throw new ArgumentNullException(nameof(topology));
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

        RebusCommandDestinationDiagnostic diagnostic =
            _topology.DescribeDestination(normalizedTargetModule);

        return diagnostic.DestinationAddress
            ?? throw new InvalidOperationException(diagnostic.FailureReason);
    }
}
