using Bondstone.Utility;

namespace Bondstone.Transport.Rebus.Inbox;

public sealed class RebusModuleCommandEndpointHandlerOptions
{
    public RebusModuleCommandEndpointHandlerOptions(string endpointName)
    {
        EndpointName = endpointName.NormalizeRequired(
            nameof(endpointName),
            "Rebus receive endpoint name");
    }

    public string EndpointName { get; }
}
