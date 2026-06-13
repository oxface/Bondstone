namespace Bondstone.Transport.Local.Outbox;

public sealed class BondstoneLocalModuleRouteBuilder(
    BondstoneLocalTransportBuilder transportBuilder,
    string targetModule)
{
    public BondstoneLocalTransportBuilder ToQueue(
        string queueName)
    {
        transportBuilder.SetModuleQueueName(targetModule, queueName);
        return transportBuilder;
    }
}
