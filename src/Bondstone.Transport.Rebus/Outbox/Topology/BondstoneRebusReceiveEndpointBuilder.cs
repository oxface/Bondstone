namespace Bondstone.Transport.Rebus.Outbox;

public sealed class BondstoneRebusReceiveEndpointBuilder
{
    internal BondstoneRebusReceiveEndpointBuilder(
        BondstoneRebusTransportBuilder transportBuilder,
        string endpointName)
    {
        _transportBuilder = transportBuilder;
        _endpointName = endpointName;
    }

    private readonly BondstoneRebusTransportBuilder _transportBuilder;
    private readonly string _endpointName;

    public BondstoneRebusTransportBuilder AcceptModule(string moduleName)
    {
        _transportBuilder.AcceptModuleOnEndpoint(_endpointName, moduleName);
        return _transportBuilder;
    }

    public BondstoneRebusTransportBuilder SubscribeEvent(
        string messageTypeName,
        string subscriberModule,
        string subscriberIdentity)
    {
        _transportBuilder.SubscribeEventOnEndpoint(
            _endpointName,
            messageTypeName,
            subscriberModule,
            subscriberIdentity);

        return _transportBuilder;
    }
}
