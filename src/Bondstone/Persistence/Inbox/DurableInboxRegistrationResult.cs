namespace Bondstone.Persistence;

public sealed record DurableInboxRegistrationResult
{
    public DurableInboxRegistrationResult(
        DurableInboxRegistrationStatus status,
        DurableInboxRecord record)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Inbox registration status is not supported.");
        }

        ArgumentNullException.ThrowIfNull(record);

        Status = status;
        Record = record;
    }

    public DurableInboxRegistrationStatus Status { get; }

    public DurableInboxRecord Record { get; }

    public bool IsRegistered => Status == DurableInboxRegistrationStatus.Registered;

    public bool IsDuplicate => !IsRegistered;
}
