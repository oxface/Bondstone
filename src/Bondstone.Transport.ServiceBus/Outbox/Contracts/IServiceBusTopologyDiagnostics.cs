using Bondstone.Transport.ServiceBus.Inbox;

namespace Bondstone.Transport.ServiceBus.Outbox;

public interface IServiceBusTopologyDiagnostics
{
    ServiceBusCommandDestinationDiagnostic DescribeCommandDestination(
        string targetModule);

    ServiceBusEventDestinationDiagnostic DescribeEventDestination(
        string messageTypeName);

    ServiceBusReceiveSourceDiagnostic DescribeReceiveSource(
        ServiceBusReceiveSource source);
}
