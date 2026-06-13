using Bondstone.Messaging;
using Bondstone.Utility;

namespace Bondstone.Configuration;

public sealed class DurableTransportTopologyRouteDiagnostic
{
    public DurableTransportTopologyRouteDiagnostic(
        string transportName,
        MessageKind messageKind,
        string routeSubject,
        bool hasRoute,
        string? failureReason = null)
    {
        TransportName = transportName.NormalizeRequired(
            nameof(transportName),
            "Transport name");
        MessageKind = messageKind;
        RouteSubject = routeSubject.NormalizeRequired(
            nameof(routeSubject),
            "Route subject");
        HasRoute = hasRoute;
        FailureReason = failureReason;
    }

    public string TransportName { get; }

    public MessageKind MessageKind { get; }

    public string RouteSubject { get; }

    public bool HasRoute { get; }

    public string? FailureReason { get; }
}
