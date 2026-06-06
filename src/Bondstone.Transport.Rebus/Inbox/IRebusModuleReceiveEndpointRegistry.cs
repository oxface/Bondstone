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
