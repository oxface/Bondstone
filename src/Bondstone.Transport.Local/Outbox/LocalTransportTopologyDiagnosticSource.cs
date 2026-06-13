using Bondstone.Configuration;
using Bondstone.Messaging;

namespace Bondstone.Transport.Local.Outbox;

internal sealed class LocalTransportTopologyDiagnosticSource(
    LocalTransportTopology topology)
    : IDurableTransportTopologyDiagnosticSource
{
    private readonly LocalTransportTopology _topology =
        topology ?? throw new ArgumentNullException(nameof(topology));

    public string TransportName => "Local";

    public DurableTransportTopologyRouteDiagnostic DescribeCommandRoute(
        string targetModule)
    {
        bool hasRoute = _topology.HasCommandRoute(targetModule);
        return new DurableTransportTopologyRouteDiagnostic(
            TransportName,
            MessageKind.Command,
            targetModule,
            hasRoute,
            hasRoute
                ? null
                : $"Local transport has no queue binding for target module '{targetModule}'.");
    }

    public DurableTransportTopologyRouteDiagnostic DescribeEventRoute(
        string messageTypeName)
    {
        bool hasRoute = _topology.HasEventRoute(messageTypeName);
        return new DurableTransportTopologyRouteDiagnostic(
            TransportName,
            MessageKind.Event,
            messageTypeName,
            hasRoute,
            hasRoute
                ? null
                : $"Local transport has no subscriber queue binding for event '{messageTypeName}'.");
    }
}
