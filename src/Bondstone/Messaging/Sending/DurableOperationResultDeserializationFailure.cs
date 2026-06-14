namespace Bondstone.Messaging;

public sealed record DurableOperationResultDeserializationFailure
{
    public DurableOperationResultDeserializationFailure(
        Guid durableOperationId,
        string resultTypeName,
        string message,
        string? exceptionTypeName = null)
    {
        if (durableOperationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Durable operation id must not be empty.",
                nameof(durableOperationId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(resultTypeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        DurableOperationId = durableOperationId;
        ResultTypeName = resultTypeName;
        Message = message;
        ExceptionTypeName = string.IsNullOrWhiteSpace(exceptionTypeName)
            ? null
            : exceptionTypeName;
    }

    public Guid DurableOperationId { get; }

    public string ResultTypeName { get; }

    public string Message { get; }

    public string? ExceptionTypeName { get; }
}
