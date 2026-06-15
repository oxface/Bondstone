using Bondstone.Messaging;

namespace Bondstone.Persistence.EntityFrameworkCore.Operations;

public sealed class OperationStateEntity
{
    private OperationStateEntity(
        Guid durableOperationId,
        DurableOperationStatus status,
        DateTimeOffset updatedAtUtc,
        string? resultPayload,
        string? failureReason,
        string? moduleName,
        string? messageTypeName,
        string? handlerIdentity)
    {
        DurableOperationId = durableOperationId;
        Status = status;
        UpdatedAtUtc = updatedAtUtc;
        ResultPayload = resultPayload;
        FailureReason = failureReason;
        ModuleName = moduleName;
        MessageTypeName = messageTypeName;
        HandlerIdentity = handlerIdentity;
    }

    private OperationStateEntity()
    {
    }

    public Guid DurableOperationId { get; private set; }

    public DurableOperationStatus Status { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public string? ResultPayload { get; private set; }

    public string? FailureReason { get; private set; }

    public string? ModuleName { get; private set; }

    public string? MessageTypeName { get; private set; }

    public string? HandlerIdentity { get; private set; }

    public static OperationStateEntity FromState(DurableOperationState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return new OperationStateEntity(
            state.DurableOperationId,
            state.Status,
            state.UpdatedAtUtc,
            state.ResultPayload,
            state.FailureReason,
            state.DiagnosticContext?.ModuleName,
            state.DiagnosticContext?.MessageTypeName,
            state.DiagnosticContext?.HandlerIdentity);
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
        ModuleName = state.DiagnosticContext?.ModuleName;
        MessageTypeName = state.DiagnosticContext?.MessageTypeName;
        HandlerIdentity = state.DiagnosticContext?.HandlerIdentity;
    }

    public DurableOperationState ToState()
    {
        return new DurableOperationState(
            DurableOperationId,
            Status,
            UpdatedAtUtc,
            ResultPayload,
            FailureReason,
            CreateDiagnosticContext());
    }

    private DurableOperationDiagnosticContext? CreateDiagnosticContext()
    {
        if (ModuleName is null
            && MessageTypeName is null
            && HandlerIdentity is null)
        {
            return null;
        }

        return new DurableOperationDiagnosticContext(
            ModuleName,
            MessageTypeName,
            HandlerIdentity);
    }
}
