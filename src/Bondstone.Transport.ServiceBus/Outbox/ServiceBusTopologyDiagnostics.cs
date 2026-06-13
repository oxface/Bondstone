using Bondstone.Transport.ServiceBus.Inbox;

namespace Bondstone.Transport.ServiceBus.Outbox;

internal sealed class ServiceBusTopologyDiagnostics(
    ServiceBusCommandDestinationTopology commandTopology,
    ServiceBusEventDestinationTopology eventDestinationTopology,
    ServiceBusReceiveTopology receiveTopology)
    : IServiceBusTopologyDiagnostics
{
    public ServiceBusCommandDestinationDiagnostic DescribeCommandDestination(
        string targetModule)
    {
        return commandTopology.DescribeDestination(targetModule);
    }

    public ServiceBusEventDestinationDiagnostic DescribeEventDestination(
        string messageTypeName)
    {
        return eventDestinationTopology.DescribeDestination(messageTypeName);
    }

    public ServiceBusReceiveSourceDiagnostic DescribeReceiveSource(
        ServiceBusReceiveSource source)
    {
        return receiveTopology.DescribeSource(source);
    }
}
