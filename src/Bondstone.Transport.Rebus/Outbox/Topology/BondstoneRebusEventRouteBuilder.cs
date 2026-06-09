namespace Bondstone.Transport.Rebus.Outbox;

public sealed class BondstoneRebusEventRouteBuilder
{
    private readonly BondstoneRebusTransportBuilder _transportBuilder;
    private readonly string _messageTypeName;

    internal BondstoneRebusEventRouteBuilder(
        BondstoneRebusTransportBuilder transportBuilder,
        string messageTypeName)
    {
        _transportBuilder = transportBuilder;
        _messageTypeName = messageTypeName;
    }

    public BondstoneRebusTransportBuilder ToTopic(string topicName)
    {
        _transportBuilder.SetEventTopicName(_messageTypeName, topicName);
        return _transportBuilder;
    }
}
