namespace Bondstone.Transport.Rebus.Outbox;

internal sealed class RebusCommandTopologyDiagnostics(
    RebusCommandDestinationTopology topology)
    : IRebusCommandTopologyDiagnostics
{
    public RebusCommandDestinationDiagnostic DescribeCommandDestination(
        string targetModule)
    {
        return topology.DescribeDestination(targetModule);
    }
}
