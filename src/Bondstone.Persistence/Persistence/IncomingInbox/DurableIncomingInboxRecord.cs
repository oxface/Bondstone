using Bondstone.Messaging;
using Bondstone.Utility;

namespace Bondstone.Persistence;

public sealed record DurableIncomingInboxRecord
{
    public DurableIncomingInboxRecord(
        DurableIncomingInboxKey key,
        DurableMessageEnvelope envelope,
        DateTimeOffset ingestedAtUtc,
        DurableIncomingInboxState? state = null,
        string? sourceTransportName = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(envelope);
        ValidateUtcTimestamp(ingestedAtUtc, nameof(ingestedAtUtc), "Ingested timestamp");
        ValidateKeyMatchesEnvelope(key, envelope);

        DurableIncomingInboxState normalizedState = state
            ?? DurableIncomingInboxState.Pending;
        ValidateNotBeforeIngested(
            normalizedState.NextAttemptAtUtc,
            ingestedAtUtc,
            nameof(state),
            "Next-attempt timestamp");
        ValidateNotBeforeIngested(
            normalizedState.ProcessedAtUtc,
            ingestedAtUtc,
            nameof(state),
            "Processed timestamp");
        ValidateNotBeforeIngested(
            normalizedState.FailedAtUtc,
            ingestedAtUtc,
            nameof(state),
            "Failed timestamp");

        Key = key;
        Envelope = envelope;
        IngestedAtUtc = ingestedAtUtc;
        State = normalizedState;
        SourceTransportName = sourceTransportName.NormalizeOptional();
    }

    public DurableIncomingInboxKey Key { get; }

    public DurableMessageEnvelope Envelope { get; }

    public string ReceiverModule => Key.ReceiverModule;

    public string HandlerIdentity => Key.HandlerIdentity;

    public string? SourceTransportName { get; }

    public DateTimeOffset IngestedAtUtc { get; }

    public DurableIncomingInboxState State { get; }

    private static void ValidateKeyMatchesEnvelope(
        DurableIncomingInboxKey key,
        DurableMessageEnvelope envelope)
    {
        if (key.MessageId != envelope.MessageId)
        {
            throw new ArgumentException(
                "Durable incoming inbox key message id must match the envelope message id.",
                nameof(key));
        }

        if (envelope.MessageKind == MessageKind.Command
            && !string.Equals(
                key.ReceiverModule,
                envelope.TargetModule,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Command durable incoming inbox key receiver module must match the envelope target module.",
                nameof(key));
        }
    }

    private static void ValidateUtcTimestamp(
        DateTimeOffset value,
        string parameterName,
        string valueName)
    {
        if (value == default)
        {
            throw new ArgumentException($"{valueName} must not be the default value.", parameterName);
        }

        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException($"{valueName} must use UTC offset.", parameterName);
        }
    }

    private static void ValidateNotBeforeIngested(
        DateTimeOffset? value,
        DateTimeOffset ingestedAtUtc,
        string parameterName,
        string valueName)
    {
        if (value is not null && value < ingestedAtUtc)
        {
            throw new ArgumentException(
                $"{valueName} must not be earlier than ingested timestamp.",
                parameterName);
        }
    }
}
