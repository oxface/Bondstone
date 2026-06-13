using Bondstone.Transport.RabbitMq.Inbox;

namespace Bondstone.Transport.RabbitMq.Outbox;

public interface IRabbitMqTopologyDiagnostics
{
    RabbitMqCommandRoutingDiagnostic DescribeCommandRoute(
        string targetModule);

    RabbitMqEventRoutingDiagnostic DescribeEventRoute(
        string messageTypeName);

    RabbitMqReceiveQueueDiagnostic DescribeReceiveQueue(
        string queueName);
}
