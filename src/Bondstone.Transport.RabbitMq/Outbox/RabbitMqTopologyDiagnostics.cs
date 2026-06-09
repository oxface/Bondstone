namespace Bondstone.Transport.RabbitMq.Outbox;

internal sealed class RabbitMqTopologyDiagnostics(
    RabbitMqCommandRoutingTopology commandTopology,
    RabbitMqEventRoutingTopology eventTopology)
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
}
