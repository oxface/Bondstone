using Bondstone.Messaging;

namespace Bondstone.Persistence.Postgres.Operations;

internal sealed class PostgresOperationStateRow
{
    public Guid DurableOperationId { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTimeOffset UpdatedAtUtc { get; init; }

    public string? ResultPayload { get; init; }

    public string? FailureReason { get; init; }

    public string? ModuleName { get; init; }

    public string? MessageTypeName { get; init; }

    public string? HandlerIdentity { get; init; }

    public DurableOperationState ToState()
    {
        return new DurableOperationState(
            DurableOperationId,
            Enum.Parse<DurableOperationStatus>(Status),
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
