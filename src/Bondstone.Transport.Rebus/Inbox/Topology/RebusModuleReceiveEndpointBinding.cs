using Bondstone.Utility;

namespace Bondstone.Transport.Rebus.Inbox;

public sealed class RebusModuleReceiveEndpointBinding
{
    public RebusModuleReceiveEndpointBinding(
        string endpointName,
        IEnumerable<string> moduleNames)
    {
        EndpointName = endpointName.NormalizeRequired(
            nameof(endpointName),
            "Rebus receive endpoint name");

        ArgumentNullException.ThrowIfNull(moduleNames);

        ModuleNames = moduleNames
            .Select(static moduleName => moduleName.NormalizeRequired(
                nameof(moduleNames),
                "Module name"))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public string EndpointName { get; }

    public IReadOnlyCollection<string> ModuleNames { get; }
}
