using Bondstone.Messaging;

namespace Bondstone.Configuration;

public interface IDurableTransportTopologyDiagnosticSource
{
    string TransportName { get; }

    DurableTransportTopologyRouteDiagnostic DescribeCommandRoute(
        string targetModule);

    DurableTransportTopologyRouteDiagnostic DescribeEventRoute(
        string messageTypeName);
}
