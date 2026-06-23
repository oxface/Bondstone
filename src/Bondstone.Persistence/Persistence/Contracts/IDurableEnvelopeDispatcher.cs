namespace Bondstone.Persistence;

public interface IDurableEnvelopeDispatcher
{
    ValueTask DispatchAsync(
        DurableOutboxRecord record,
        CancellationToken ct = default);
}
