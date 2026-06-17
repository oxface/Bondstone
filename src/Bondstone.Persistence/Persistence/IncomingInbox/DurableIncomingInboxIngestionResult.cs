namespace Bondstone.Persistence;

public sealed record DurableIncomingInboxIngestionResult
{
    public DurableIncomingInboxIngestionResult(
        DurableIncomingInboxIngestionStatus status,
        DurableIncomingInboxRecord record)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Durable incoming inbox ingestion status is not supported.");
        }

        Status = status;
        Record = record ?? throw new ArgumentNullException(nameof(record));
    }

    public DurableIncomingInboxIngestionStatus Status { get; }

    public DurableIncomingInboxRecord Record { get; }

    public bool WasIngested => Status == DurableIncomingInboxIngestionStatus.Ingested;

    public bool WasAlreadyIngested => Status == DurableIncomingInboxIngestionStatus.AlreadyIngested;
}
