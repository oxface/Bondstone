namespace Bondstone.Transport.Rebus.Outbox;

public enum RebusCommandDestinationSource
{
    Missing = 0,
    ExplicitRoute = 1,
    ReceiveEndpoint = 2,
    ModuleQueueConvention = 3,
}
