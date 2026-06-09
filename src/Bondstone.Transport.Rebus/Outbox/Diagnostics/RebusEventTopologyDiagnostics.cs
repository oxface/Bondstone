namespace Bondstone.Transport.Rebus.Outbox;

internal sealed class RebusEventTopologyDiagnostics(
    RebusEventTopicTopology topology)
    : IRebusEventTopologyDiagnostics
{
    public RebusEventTopicDiagnostic DescribeEventTopic(
        string messageTypeName)
    {
        return topology.DescribeTopic(messageTypeName);
    }
}
