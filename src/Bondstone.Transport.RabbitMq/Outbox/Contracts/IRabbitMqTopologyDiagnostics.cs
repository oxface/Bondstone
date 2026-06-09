namespace Bondstone.Transport.RabbitMq.Outbox;

public interface IRabbitMqTopologyDiagnostics
{
    RabbitMqCommandRoutingDiagnostic DescribeCommandRoute(
        string targetModule);

    RabbitMqEventRoutingDiagnostic DescribeEventRoute(
        string messageTypeName);
}
