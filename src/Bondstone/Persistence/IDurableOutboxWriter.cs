using Bondstone.Messaging;

namespace Bondstone.Persistence;

public interface IDurableOutboxWriter
{
    ValueTask WriteAsync(
        DurableMessageEnvelope envelope,
        CancellationToken cancellationToken = default);
}
