namespace Bondstone.Transport.ServiceBus.Outbox;

internal sealed record ServiceBusEventDestinationConvention(
    ServiceBusEventDestinationKind Kind,
    Func<string, string> NameFactory);
