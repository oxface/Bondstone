namespace Bondstone.Persistence;

public interface IDurableOutboxTransport
{
    ValueTask SendAsync(
        DurableOutboxRecord record,
        CancellationToken cancellationToken = default);
}
