namespace Bondstone.Persistence;

public interface IDurableOutboxTransport
{
    ValueTask SendAsync(
        DurableOutboxRecord record,
        CancellationToken ct = default);
}
