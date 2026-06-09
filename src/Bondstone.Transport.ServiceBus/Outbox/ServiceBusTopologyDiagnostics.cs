namespace Bondstone.Transport.ServiceBus.Outbox;

internal sealed class ServiceBusTopologyDiagnostics(
    ServiceBusCommandDestinationTopology commandTopology,
    ServiceBusEventTopicTopology eventTopicTopology)
    : IServiceBusTopologyDiagnostics
{
    public ServiceBusCommandDestinationDiagnostic DescribeCommandDestination(
        string targetModule)
    {
        return commandTopology.DescribeDestination(targetModule);
    }

    public ServiceBusEventTopicDiagnostic DescribeEventTopic(
        string messageTypeName)
    {
        return eventTopicTopology.DescribeTopic(messageTypeName);
    }
}
