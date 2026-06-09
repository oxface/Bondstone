using Bondstone.Utility;

namespace Bondstone.Transport.Rebus.Outbox;

internal sealed class RebusCommandDestinationTopology
{
    private readonly IReadOnlyDictionary<string, string> _explicitDestinationAddressesByTargetModule;
    private readonly IReadOnlyDictionary<string, string> _receiveEndpointNamesByTargetModule;
    private readonly Func<string, string>? _destinationAddressConvention;

    public RebusCommandDestinationTopology(
        IReadOnlyDictionary<string, string> explicitDestinationAddressesByTargetModule,
        IReadOnlyDictionary<string, string> receiveEndpointNamesByTargetModule,
        Func<string, string>? destinationAddressConvention = null)
    {
        ArgumentNullException.ThrowIfNull(explicitDestinationAddressesByTargetModule);
        ArgumentNullException.ThrowIfNull(receiveEndpointNamesByTargetModule);

        _explicitDestinationAddressesByTargetModule =
            NormalizeMap(
                explicitDestinationAddressesByTargetModule,
                "targetModule",
                "Target module",
                "destinationAddress",
                "Rebus destination address");
        _receiveEndpointNamesByTargetModule =
            NormalizeMap(
                receiveEndpointNamesByTargetModule,
                "targetModule",
                "Target module",
                "receiveEndpointName",
                "Rebus receive endpoint name");
        _destinationAddressConvention = destinationAddressConvention;
    }

    public static RebusCommandDestinationTopology FromConfiguredDestinations(
        IReadOnlyDictionary<string, string> destinationAddressesByTargetModule,
        Func<string, string>? destinationAddressConvention = null)
    {
        return new RebusCommandDestinationTopology(
            destinationAddressesByTargetModule,
            new Dictionary<string, string>(),
            destinationAddressConvention);
    }

    public RebusCommandDestinationDiagnostic DescribeDestination(
        string targetModule)
    {
        string normalizedTargetModule = targetModule.NormalizeRequired(
            nameof(targetModule),
            "Target module");

        if (_explicitDestinationAddressesByTargetModule.TryGetValue(
            normalizedTargetModule,
            out string? explicitDestinationAddress))
        {
            return new RebusCommandDestinationDiagnostic(
                normalizedTargetModule,
                RebusCommandDestinationSource.ExplicitRoute,
                explicitDestinationAddress);
        }

        if (_receiveEndpointNamesByTargetModule.TryGetValue(
            normalizedTargetModule,
            out string? receiveEndpointName))
        {
            return new RebusCommandDestinationDiagnostic(
                normalizedTargetModule,
                RebusCommandDestinationSource.ReceiveEndpoint,
                receiveEndpointName,
                receiveEndpointName);
        }

        if (_destinationAddressConvention is not null)
        {
            string destinationAddress = _destinationAddressConvention(normalizedTargetModule)
                .NormalizeRequired(
                    nameof(_destinationAddressConvention),
                    "Rebus destination address");

            return new RebusCommandDestinationDiagnostic(
                normalizedTargetModule,
                RebusCommandDestinationSource.ModuleQueueConvention,
                destinationAddress);
        }

        return new RebusCommandDestinationDiagnostic(
            normalizedTargetModule,
            RebusCommandDestinationSource.Missing,
            failureReason:
                $"No Rebus destination address is configured for target module '{normalizedTargetModule}'.");
    }

    private static IReadOnlyDictionary<string, string> NormalizeMap(
        IReadOnlyDictionary<string, string> map,
        string keyParameterName,
        string keyValueName,
        string valueParameterName,
        string valueName)
    {
        return map
            .Select(entry => new KeyValuePair<string, string>(
                entry.Key.NormalizeRequired(keyParameterName, keyValueName),
                entry.Value.NormalizeRequired(valueParameterName, valueName)))
            .ToDictionary(
                static entry => entry.Key,
                static entry => entry.Value,
                StringComparer.Ordinal);
    }
}
