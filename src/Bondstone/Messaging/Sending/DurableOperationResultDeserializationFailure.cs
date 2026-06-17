namespace Bondstone.Messaging;

/// <summary>
/// Describes a completed durable operation result payload that could not be
/// deserialized as the requested result type.
/// </summary>
/// <remarks>
/// A deserialization failure does not change the stored operation status. The
/// operation can still be completed while the caller cannot read the stored
/// payload as the requested type.
/// </remarks>
public sealed record DurableOperationResultDeserializationFailure
{
    public DurableOperationResultDeserializationFailure(
        Guid durableOperationId,
        string resultTypeName,
        string message,
        string? exceptionTypeName = null,
        DurableOperationDiagnosticContext? diagnosticContext = null)
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
        DiagnosticContext = diagnosticContext;
    }

    /// <summary>
    /// Gets the durable operation identifier whose result payload failed to
    /// deserialize.
    /// </summary>
    public Guid DurableOperationId { get; }

    /// <summary>
    /// Gets the requested result type name.
    /// </summary>
    public string ResultTypeName { get; }

    /// <summary>
    /// Gets the diagnostic message for the deserialization failure.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the serializer exception type name when available.
    /// </summary>
    public string? ExceptionTypeName { get; }

    /// <summary>
    /// Gets optional diagnostic context captured with operation state.
    /// </summary>
    public DurableOperationDiagnosticContext? DiagnosticContext { get; }
}
