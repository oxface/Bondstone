namespace Bondstone.Persistence;

public interface IDurableEnvelopeDispatchRoute
{
    string TransportName { get; }

    bool CanSend(
        DurableOutboxRecord record);

    ValueTask DispatchAsync(
        DurableOutboxRecord record,
        CancellationToken ct = default);
}
