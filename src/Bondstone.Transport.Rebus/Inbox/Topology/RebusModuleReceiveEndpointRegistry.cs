using Bondstone.Utility;

namespace Bondstone.Transport.Rebus.Inbox;

public interface IRebusModuleReceiveEndpointRegistry
{
    IReadOnlyCollection<RebusModuleReceiveEndpointBinding> Endpoints { get; }

    RebusModuleReceiveEndpointBinding GetEndpoint(string endpointName);

    bool EndpointAcceptsModule(
        string endpointName,
        string moduleName);

    bool TryGetEndpointNameForModule(
        string moduleName,
        out string? endpointName);
}

public sealed class RebusModuleReceiveEndpointRegistry : IRebusModuleReceiveEndpointRegistry
{
    private readonly IReadOnlyDictionary<string, RebusModuleReceiveEndpointBinding> _endpointsByName;
    private readonly IReadOnlyDictionary<string, string> _endpointNamesByModuleName;

    public RebusModuleReceiveEndpointRegistry(
        IEnumerable<RebusModuleReceiveEndpointBinding> endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var moduleNamesByEndpointName =
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var endpointNamesByModuleName =
            new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (RebusModuleReceiveEndpointBinding endpoint in endpoints)
        {
            if (!moduleNamesByEndpointName.TryGetValue(
                endpoint.EndpointName,
                out HashSet<string>? moduleNames))
            {
                moduleNames = new HashSet<string>(StringComparer.Ordinal);
                moduleNamesByEndpointName.Add(endpoint.EndpointName, moduleNames);
            }

            foreach (string moduleName in endpoint.ModuleNames)
            {
                AddModuleEndpoint(endpoint.EndpointName, moduleName, endpointNamesByModuleName);
                moduleNames.Add(moduleName);
            }
        }

        _endpointsByName = moduleNamesByEndpointName
            .Select(static entry => new RebusModuleReceiveEndpointBinding(
                entry.Key,
                entry.Value))
            .ToDictionary(
                static endpoint => endpoint.EndpointName,
                static endpoint => endpoint,
                StringComparer.Ordinal);
        _endpointNamesByModuleName = endpointNamesByModuleName;
    }

    public IReadOnlyCollection<RebusModuleReceiveEndpointBinding> Endpoints =>
        _endpointsByName.Values.ToArray();

    public RebusModuleReceiveEndpointBinding GetEndpoint(string endpointName)
    {
        string normalizedEndpointName = endpointName.NormalizeRequired(
            nameof(endpointName),
            "Rebus receive endpoint name");

        if (_endpointsByName.TryGetValue(
            normalizedEndpointName,
            out RebusModuleReceiveEndpointBinding? endpoint))
        {
            return endpoint;
        }

        throw new InvalidOperationException(
            $"Rebus receive endpoint '{normalizedEndpointName}' is not configured.");
    }

    public bool EndpointAcceptsModule(
        string endpointName,
        string moduleName)
    {
        string normalizedEndpointName = endpointName.NormalizeRequired(
            nameof(endpointName),
            "Rebus receive endpoint name");
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");

        return _endpointNamesByModuleName.TryGetValue(
                normalizedModuleName,
                out string? acceptedEndpointName)
            && acceptedEndpointName == normalizedEndpointName;
    }

    public bool TryGetEndpointNameForModule(
        string moduleName,
        out string? endpointName)
    {
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");

        return _endpointNamesByModuleName.TryGetValue(
            normalizedModuleName,
            out endpointName);
    }

    private static void AddModuleEndpoint(
        string endpointName,
        string moduleName,
        Dictionary<string, string> endpointNamesByModuleName)
    {
        if (endpointNamesByModuleName.TryGetValue(
            moduleName,
            out string? existingEndpointName))
        {
            if (existingEndpointName == endpointName)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Module '{moduleName}' is already accepted by Rebus receive endpoint '{existingEndpointName}'.");
        }

        endpointNamesByModuleName.Add(moduleName, endpointName);
    }
}
