namespace Bondstone.Persistence;

public sealed record DurableInboxHandleResult
{
    public DurableInboxHandleResult(
        DurableInboxHandleStatus status,
        DurableInboxRecord record)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Inbox handle status is not supported.");
        }

        Status = status;
        Record = record ?? throw new ArgumentNullException(nameof(record));
    }

    public DurableInboxHandleStatus Status { get; }

    public DurableInboxRecord Record { get; }

    public bool WasHandled => Status == DurableInboxHandleStatus.Handled;

    public bool WasSkipped => !WasHandled;
}
