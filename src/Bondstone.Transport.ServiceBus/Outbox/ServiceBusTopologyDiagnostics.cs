namespace Bondstone.Transport.ServiceBus.Outbox;

internal sealed class ServiceBusTopologyDiagnostics(
    ServiceBusCommandDestinationTopology commandTopology,
    ServiceBusEventDestinationTopology eventDestinationTopology)
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
}
