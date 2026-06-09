using Bondstone.Messaging;

namespace Bondstone.Persistence.Dapper.Postgres.Operations;

internal sealed class PostgresDapperOperationStateRow
{
    public Guid DurableOperationId { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTimeOffset UpdatedAtUtc { get; init; }

    public string? ResultPayload { get; init; }

    public string? FailureReason { get; init; }

    public DurableOperationState ToState()
    {
        return new DurableOperationState(
            DurableOperationId,
            Enum.Parse<DurableOperationStatus>(Status),
            UpdatedAtUtc,
            ResultPayload,
            FailureReason);
    }
}
