namespace Bondstone.Transport.ServiceBus.Inbox;

public sealed class ServiceBusReceiveWorkerOptions
{
    public int MaxConcurrentCalls { get; set; } = 1;

    public TimeSpan MaxAutoLockRenewalDuration { get; set; } = TimeSpan.FromMinutes(5);

    public void Validate()
    {
        if (MaxConcurrentCalls <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaxConcurrentCalls),
                MaxConcurrentCalls,
                "Service Bus receive max concurrent calls must be positive.");
        }

        if (MaxAutoLockRenewalDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaxAutoLockRenewalDuration),
                MaxAutoLockRenewalDuration,
                "Service Bus receive max auto lock renewal duration must be positive.");
        }
    }
}
