namespace Bondstone.Transport.RabbitMq.Inbox;

public sealed class RabbitMqReceiveWorkerOptions
{
    public ushort PrefetchCount { get; set; } = 10;

    public bool RequeueOnFailure { get; set; }

    public void Validate()
    {
        if (PrefetchCount == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(PrefetchCount),
                PrefetchCount,
                "RabbitMQ receive prefetch count must be positive.");
        }
    }
}
