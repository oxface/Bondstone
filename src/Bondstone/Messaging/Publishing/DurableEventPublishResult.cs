namespace Bondstone.Messaging;

public sealed record DurableEventPublishResult
{
    public DurableEventPublishResult(
        Guid publishId,
        Guid? durableOperationId,
        DurableEventPublishStatus status)
    {
        if (publishId == Guid.Empty)
        {
            throw new ArgumentException("Publish id must not be empty.", nameof(publishId));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Publish status is not supported.");
        }

        PublishId = publishId;
        DurableOperationId = durableOperationId;
        Status = status;
    }

    public Guid PublishId { get; }

    public Guid? DurableOperationId { get; }

    public DurableEventPublishStatus Status { get; }
}
