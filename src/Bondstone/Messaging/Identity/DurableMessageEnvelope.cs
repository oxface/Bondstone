using Bondstone.Utility;

namespace Bondstone.Messaging;

public sealed record DurableMessageEnvelope
{
    public DurableMessageEnvelope(
        Guid messageId,
        MessageKind messageKind,
        string messageTypeName,
        string sourceModule,
        string? targetModule,
        string payload,
        DateTimeOffset createdAtUtc,
        Guid? durableOperationId = null,
        MessageTraceContext? traceContext = null,
        Guid? causationId = null,
        string? partitionKey = null,
        string? metadata = null)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException("Message id must not be empty.", nameof(messageId));
        }

        if (!Enum.IsDefined(messageKind))
        {
            throw new ArgumentOutOfRangeException(nameof(messageKind), messageKind, "Message kind is not supported.");
        }

        if (createdAtUtc == default)
        {
            throw new ArgumentException("Created timestamp must not be the default value.", nameof(createdAtUtc));
        }

        if (createdAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Created timestamp must use UTC offset.", nameof(createdAtUtc));
        }

        MessageId = messageId;
        MessageKind = messageKind;
        MessageTypeName = messageTypeName.NormalizeRequired(nameof(messageTypeName), "Message type name");
        SourceModule = sourceModule.NormalizeRequired(nameof(sourceModule), "Source module");
        TargetModule = NormalizeTargetModule(messageKind, targetModule);
        Payload = RequireContent(payload, nameof(payload), "Payload");
        CreatedAtUtc = createdAtUtc;
        DurableOperationId = durableOperationId;
        TraceContext = traceContext;
        CausationId = causationId;
        PartitionKey = partitionKey.NormalizeOptional();
        Metadata = string.IsNullOrWhiteSpace(metadata)
            ? null
            : metadata;
    }

    public Guid MessageId { get; }

    public MessageKind MessageKind { get; }

    public string MessageTypeName { get; }

    public string SourceModule { get; }

    public string? TargetModule { get; }

    public Guid? DurableOperationId { get; }

    public MessageTraceContext? TraceContext { get; }

    public Guid? CausationId { get; }

    public string? PartitionKey { get; }

    public string Payload { get; }

    public string? Metadata { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    private static string? NormalizeTargetModule(MessageKind messageKind, string? targetModule)
    {
        string? normalizedTargetModule = targetModule.NormalizeOptional();

        if (messageKind == MessageKind.Command && normalizedTargetModule is null)
        {
            throw new ArgumentException("Command messages require a target module.", nameof(targetModule));
        }

        if (messageKind == MessageKind.Event && normalizedTargetModule is not null)
        {
            throw new ArgumentException("Event messages must not specify a target module.", nameof(targetModule));
        }

        return normalizedTargetModule;
    }

    private static string RequireContent(string? value, string parameterName, string valueName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{valueName} is required.", parameterName);
        }

        return value;
    }
}
