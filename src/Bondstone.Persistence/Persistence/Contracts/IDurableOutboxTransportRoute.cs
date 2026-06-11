namespace Bondstone.Persistence;

public interface IDurableOutboxTransportRoute
{
    string TransportName { get; }

    bool CanSend(
        DurableOutboxRecord record);

    ValueTask SendAsync(
        DurableOutboxRecord record,
        CancellationToken ct = default);
}
