using Bondstone.Persistence;
using Rebus.Bus;

namespace Bondstone.Transport.Rebus.Outbox;

internal sealed class RebusEventOutboxTransport(
    IBus bus,
    IRebusOutboxEventTopicResolver eventTopicResolver)
{
    public async ValueTask PublishAsync(DurableOutboxRecord record)
    {
        string topicName = eventTopicResolver.ResolveTopic(record);
        RebusDurableMessageEnvelope rebusEnvelope =
            RebusDurableEnvelopeMapper.CreateEnvelope(record.Envelope);
        Dictionary<string, string> headers =
            RebusDurableEnvelopeMapper.CreateHeaders(record.Envelope);

        await bus.Advanced.Topics.Publish(topicName, rebusEnvelope, headers);
    }
}
