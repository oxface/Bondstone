namespace Bondstone.Transport.Rebus.Outbox;

public interface IRebusCommandTopologyDiagnostics
{
    RebusCommandDestinationDiagnostic DescribeCommandDestination(
        string targetModule);
}
