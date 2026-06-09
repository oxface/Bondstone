namespace Bondstone.Messaging;

public enum DurableMessageTopologyDiagnosticKind
{
    CommandRoute = 1,
    CommandDestination = 2,
    CommandReceiveEndpoint = 3,
    EventTopic = 4,
    EventSubscription = 5,
}
