namespace Bondstone.Messaging;

public sealed record DurableCommandSendResult
{
    public DurableCommandSendResult(
        Guid sendId,
        Guid? durableOperationId,
        DurableCommandSendStatus status)
    {
        if (sendId == Guid.Empty)
        {
            throw new ArgumentException("Send id must not be empty.", nameof(sendId));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Send status is not supported.");
        }

        SendId = sendId;
        DurableOperationId = durableOperationId;
        Status = status;
    }

    public Guid SendId { get; }

    public Guid? DurableOperationId { get; }

    public DurableCommandSendStatus Status { get; }
}
