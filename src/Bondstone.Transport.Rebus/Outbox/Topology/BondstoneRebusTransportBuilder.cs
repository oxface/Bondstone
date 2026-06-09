using Bondstone.Transport.Rebus.Inbox;
using Bondstone.Utility;

namespace Bondstone.Transport.Rebus.Outbox;

public sealed class BondstoneRebusTransportBuilder
{
    private readonly Dictionary<string, string> _destinationAddressesByTargetModule =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _acceptedModuleNamesByEndpointName =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _endpointNamesByAcceptedModuleName =
        new(StringComparer.Ordinal);
    private Func<string, string>? _moduleQueueNameConvention;

    internal IReadOnlyCollection<RebusModuleReceiveEndpointBinding> ReceiveEndpointBindings =>
        _acceptedModuleNamesByEndpointName
            .Where(static entry => entry.Value.Count > 0)
            .Select(static entry => new RebusModuleReceiveEndpointBinding(
                entry.Key,
                entry.Value))
            .ToArray();

    internal RebusCommandDestinationTopology CommandDestinationTopology =>
        new(
            _destinationAddressesByTargetModule,
            _endpointNamesByAcceptedModuleName,
            _moduleQueueNameConvention);

    public BondstoneRebusModuleRouteBuilder RouteModule(string targetModule)
    {
        string normalizedTargetModule = targetModule.NormalizeRequired(
            nameof(targetModule),
            "Target module");

        return new BondstoneRebusModuleRouteBuilder(this, normalizedTargetModule);
    }

    public BondstoneRebusTransportBuilder UseModuleQueueConvention()
    {
        return UseModuleQueueConvention(static moduleName => $"{moduleName}-commands");
    }

    public BondstoneRebusTransportBuilder UseModuleQueueConvention(
        Func<string, string> queueNameFactory)
    {
        ArgumentNullException.ThrowIfNull(queueNameFactory);

        _moduleQueueNameConvention = moduleName =>
            queueNameFactory(moduleName).NormalizeRequired(
                nameof(queueNameFactory),
                "Rebus module queue name");

        return this;
    }

    public BondstoneRebusTransportBuilder ReceiveModule(string moduleName)
    {
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");

        if (_moduleQueueNameConvention is null)
        {
            throw new InvalidOperationException(
                $"Receiving module '{normalizedModuleName}' by convention requires {nameof(UseModuleQueueConvention)} to be configured first.");
        }

        string endpointName = _moduleQueueNameConvention(normalizedModuleName);
        AcceptModuleOnEndpoint(endpointName, normalizedModuleName);
        return this;
    }

    public BondstoneRebusReceiveEndpointBuilder ReceiveEndpoint(string endpointName)
    {
        string normalizedEndpointName = endpointName.NormalizeRequired(
            nameof(endpointName),
            "Rebus receive endpoint name");

        if (!_acceptedModuleNamesByEndpointName.ContainsKey(normalizedEndpointName))
        {
            _acceptedModuleNamesByEndpointName.Add(
                normalizedEndpointName,
                new HashSet<string>(StringComparer.Ordinal));
        }

        return new BondstoneRebusReceiveEndpointBuilder(this, normalizedEndpointName);
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

    internal void AcceptModuleOnEndpoint(
        string endpointName,
        string moduleName)
    {
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");

        if (_endpointNamesByAcceptedModuleName.TryGetValue(
            normalizedModuleName,
            out string? existingEndpointName))
        {
            if (existingEndpointName == endpointName)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Module '{normalizedModuleName}' is already accepted by Rebus receive endpoint '{existingEndpointName}'.");
        }

        if (!_acceptedModuleNamesByEndpointName.TryGetValue(
            endpointName,
            out HashSet<string>? moduleNames))
        {
            moduleNames = new HashSet<string>(StringComparer.Ordinal);
            _acceptedModuleNamesByEndpointName.Add(endpointName, moduleNames);
        }

        moduleNames.Add(normalizedModuleName);
        _endpointNamesByAcceptedModuleName.Add(normalizedModuleName, endpointName);
    }
}
