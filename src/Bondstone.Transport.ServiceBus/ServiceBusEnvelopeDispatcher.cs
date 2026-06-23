using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Microsoft.Extensions.Options;

namespace Bondstone.Transport.ServiceBus;

internal sealed class ServiceBusEnvelopeDispatcher(
    ServiceBusClient client,
    IDurableMessageEnvelopeSerializer serializer,
    IOptions<ServiceBusEnvelopeDispatcherOptions> options)
    : IDurableEnvelopeDispatcher,
        IAsyncDisposable
{
    private readonly ServiceBusClient _client =
        client ?? throw new ArgumentNullException(nameof(client));
    private readonly IDurableMessageEnvelopeSerializer _serializer =
        serializer ?? throw new ArgumentNullException(nameof(serializer));
    private readonly ServiceBusEnvelopeDispatcherOptions _options =
        options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ConcurrentDictionary<string, ServiceBusSender> _senders =
        new(StringComparer.Ordinal);

    public async ValueTask DispatchAsync(
        DurableOutboxRecord record,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        DurableMessageEnvelope envelope = record.Envelope;
        string entityName = _options.GetEntityName(envelope);
        ServiceBusSender sender = _senders.GetOrAdd(
            entityName,
            _client.CreateSender);

        var message = new ServiceBusMessage(
            BinaryData.FromBytes(_serializer.SerializeToUtf8Bytes(envelope)))
        {
            MessageId = envelope.MessageId.ToString("D"),
            Subject = envelope.MessageTypeName,
            ContentType = _options.ContentType,
            CorrelationId = envelope.DurableOperationId?.ToString("D"),
        };
        message.ApplicationProperties["bondstone-message-kind"] =
            envelope.MessageKind.ToString();
        message.ApplicationProperties["bondstone-source-module"] =
            envelope.SourceModule;
        if (envelope.TargetModule is not null)
        {
            message.ApplicationProperties["bondstone-target-module"] =
                envelope.TargetModule;
        }

        await sender.SendMessageAsync(message, ct);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (ServiceBusSender sender in _senders.Values)
        {
            await sender.DisposeAsync();
        }
    }
}
