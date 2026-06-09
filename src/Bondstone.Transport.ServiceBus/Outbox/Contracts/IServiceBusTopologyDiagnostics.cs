namespace Bondstone.Transport.ServiceBus.Outbox;

public interface IServiceBusTopologyDiagnostics
{
    ServiceBusCommandDestinationDiagnostic DescribeCommandDestination(
        string targetModule);

    ServiceBusEventTopicDiagnostic DescribeEventTopic(
        string messageTypeName);
}
