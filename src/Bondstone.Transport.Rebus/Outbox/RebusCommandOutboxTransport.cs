using Bondstone.Persistence;
using Rebus.Bus;

namespace Bondstone.Transport.Rebus.Outbox;

internal sealed class RebusCommandOutboxTransport(
    IBus bus,
    IRebusOutboxDestinationResolver destinationResolver)
{
    public async ValueTask SendAsync(DurableOutboxRecord record)
    {
        string destinationAddress = destinationResolver.ResolveDestinationAddress(record);
        RebusDurableMessageEnvelope rebusEnvelope =
            RebusDurableEnvelopeMapper.CreateEnvelope(record.Envelope);
        Dictionary<string, string> headers =
            RebusDurableEnvelopeMapper.CreateHeaders(record.Envelope);

        await bus.Advanced.Routing.Send(destinationAddress, rebusEnvelope, headers);
    }
}
