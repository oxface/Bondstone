namespace Bondstone.Messaging;

public sealed record DurableCommandSendResult
{
    public DurableCommandSendResult(
        Guid sendId,
        Guid? durableOperationId,
        DurableCommandSendStatus status)
        : this(
            sendId,
            durableOperationId,
            operation: null,
            status)
    {
    }

    public DurableCommandSendResult(
        Guid sendId,
        DurableOperationHandle? operation,
        DurableCommandSendStatus status)
        : this(
            sendId,
            operation?.DurableOperationId,
            operation,
            status)
    {
    }

    private DurableCommandSendResult(
        Guid sendId,
        Guid? durableOperationId,
        DurableOperationHandle? operation,
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
        Operation = operation;
        Status = status;
    }

    public Guid SendId { get; }

    public Guid? DurableOperationId { get; }

    public DurableOperationHandle? Operation { get; }

    public string? SourceModule => Operation?.SourceModule;

    public string? TargetModule => Operation?.TargetModule;

    public DurableCommandSendStatus Status { get; }
}
