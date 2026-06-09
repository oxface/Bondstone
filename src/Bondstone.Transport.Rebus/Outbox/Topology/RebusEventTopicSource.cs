namespace Bondstone.Transport.Rebus.Outbox;

public enum RebusEventTopicSource
{
    ExplicitRoute = 1,
    Convention = 2,
    Missing = 3,
}
