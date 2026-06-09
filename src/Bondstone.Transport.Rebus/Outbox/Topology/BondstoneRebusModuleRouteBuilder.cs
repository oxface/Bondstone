namespace Bondstone.Transport.Rebus.Outbox;

public sealed class BondstoneRebusModuleRouteBuilder
{
    internal BondstoneRebusModuleRouteBuilder(
        BondstoneRebusTransportBuilder transportBuilder,
        string targetModule)
    {
        _transportBuilder = transportBuilder;
        _targetModule = targetModule;
    }

    private readonly BondstoneRebusTransportBuilder _transportBuilder;
    private readonly string _targetModule;

    public BondstoneRebusTransportBuilder ToQueue(string queueName)
    {
        _transportBuilder.SetModuleDestinationAddress(_targetModule, queueName);
        return _transportBuilder;
    }

    public BondstoneRebusTransportBuilder ToAddress(string destinationAddress)
    {
        _transportBuilder.SetModuleDestinationAddress(
            _targetModule,
            destinationAddress);

        return _transportBuilder;
    }
}
