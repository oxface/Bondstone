using Bondstone.Transport.RabbitMq.Inbox;

namespace Bondstone.Transport.RabbitMq.Outbox;

internal sealed class RabbitMqTopologyDiagnostics(
    RabbitMqCommandRoutingTopology commandTopology,
    RabbitMqEventRoutingTopology eventTopology,
    RabbitMqReceiveTopology receiveTopology)
    : IRabbitMqTopologyDiagnostics
{
    public RabbitMqCommandRoutingDiagnostic DescribeCommandRoute(
        string targetModule)
    {
        return commandTopology.DescribeRoute(targetModule);
    }

    public RabbitMqEventRoutingDiagnostic DescribeEventRoute(
        string messageTypeName)
    {
        return eventTopology.DescribeRoute(messageTypeName);
    }

    public RabbitMqReceiveQueueDiagnostic DescribeReceiveQueue(
        string queueName)
    {
        return receiveTopology.DescribeQueue(queueName);
    }
}
