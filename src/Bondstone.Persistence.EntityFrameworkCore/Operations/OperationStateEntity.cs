using Bondstone.Messaging;

namespace Bondstone.Persistence.EntityFrameworkCore.Operations;

public sealed class OperationStateEntity
{
    private OperationStateEntity(
        Guid durableOperationId,
        DurableOperationStatus status,
        DateTimeOffset updatedAtUtc,
        string? resultPayload,
        string? failureReason)
    {
        DurableOperationId = durableOperationId;
        Status = status;
        UpdatedAtUtc = updatedAtUtc;
        ResultPayload = resultPayload;
        FailureReason = failureReason;
    }

    private OperationStateEntity()
    {
    }

    public Guid DurableOperationId { get; private set; }

    public DurableOperationStatus Status { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public string? ResultPayload { get; private set; }

    public string? FailureReason { get; private set; }

    public static OperationStateEntity FromState(DurableOperationState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return new OperationStateEntity(
            state.DurableOperationId,
            state.Status,
            state.UpdatedAtUtc,
            state.ResultPayload,
            state.FailureReason);
    }

    public void ApplyState(DurableOperationState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.DurableOperationId != DurableOperationId)
        {
            throw new InvalidOperationException(
                "Cannot apply operation state for a different durable operation id.");
        }

        Status = state.Status;
        UpdatedAtUtc = state.UpdatedAtUtc;
        ResultPayload = state.ResultPayload;
        FailureReason = state.FailureReason;
    }

    public DurableOperationState ToState()
    {
        return new DurableOperationState(
            DurableOperationId,
            Status,
            UpdatedAtUtc,
            ResultPayload,
            FailureReason);
    }
}
