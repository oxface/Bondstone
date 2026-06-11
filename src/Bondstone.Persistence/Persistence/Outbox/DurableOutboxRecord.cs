using Bondstone.Messaging;

namespace Bondstone.Persistence;

public sealed record DurableOutboxRecord
{
    public DurableOutboxRecord(
        DurableMessageEnvelope envelope,
        DateTimeOffset storedAtUtc,
        DurableOutboxDispatchState? dispatchState = null)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (storedAtUtc == default)
        {
            throw new ArgumentException("Stored timestamp must not be the default value.", nameof(storedAtUtc));
        }

        if (storedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Stored timestamp must use UTC offset.", nameof(storedAtUtc));
        }

        DurableOutboxDispatchState normalizedDispatchState = dispatchState
            ?? DurableOutboxDispatchState.Pending;
        ValidateNotBeforeStored(
            normalizedDispatchState.NextAttemptAtUtc,
            storedAtUtc,
            nameof(dispatchState),
            "Next-attempt timestamp");
        ValidateNotBeforeStored(
            normalizedDispatchState.DispatchedAtUtc,
            storedAtUtc,
            nameof(dispatchState),
            "Dispatched timestamp");
        ValidateNotBeforeStored(
            normalizedDispatchState.FailedAtUtc,
            storedAtUtc,
            nameof(dispatchState),
            "Failed timestamp");

        Envelope = envelope;
        StoredAtUtc = storedAtUtc;
        DispatchState = normalizedDispatchState;
    }

    public DurableMessageEnvelope Envelope { get; }

    public DateTimeOffset StoredAtUtc { get; }

    public DurableOutboxDispatchState DispatchState { get; }

    private static void ValidateNotBeforeStored(
        DateTimeOffset? value,
        DateTimeOffset storedAtUtc,
        string parameterName,
        string valueName)
    {
        if (value is not null && value < storedAtUtc)
        {
            throw new ArgumentException(
                $"{valueName} must not be earlier than stored timestamp.",
                parameterName);
        }
    }
}
