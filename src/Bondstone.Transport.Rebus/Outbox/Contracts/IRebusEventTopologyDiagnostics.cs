namespace Bondstone.Transport.Rebus.Outbox;

public interface IRebusEventTopologyDiagnostics
{
    RebusEventTopicDiagnostic DescribeEventTopic(
        string messageTypeName);
}
