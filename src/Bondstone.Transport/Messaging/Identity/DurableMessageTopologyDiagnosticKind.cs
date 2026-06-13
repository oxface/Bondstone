namespace Bondstone.Messaging;

public enum DurableMessageTopologyDiagnosticKind
{
    CommandRoute = 1,
    CommandDestination = 2,
    CommandReceiveEndpoint = 3,
    EventDestination = 4,
    EventSubscription = 5,
}
