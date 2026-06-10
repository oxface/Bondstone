using Bondstone.Messaging;
using Bondstone.Persistence;
using Bondstone.Persistence.Postgres.Persistence;
using Bondstone.Utility;
using Dapper;

namespace Bondstone.Persistence.Postgres.Outbox;

public sealed class PostgresDurableOutboxWriter(
    IPostgresModuleSession session,
    TimeProvider? timeProvider = null,
    string? schema = null)
    : IDurableOutboxWriter
{
    private readonly IPostgresModuleSession _session =
        session ?? throw new ArgumentNullException(nameof(session));
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly string _tableName = PostgresTableIdentifier.Build(
        PostgresDurableTableNames.OutboxMessages,
        schema);

    public async ValueTask WriteAsync(
        DurableMessageEnvelope envelope,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        DateTimeOffset storedAtUtc = _timeProvider.GetUtcNow();
        await _session.EnsureOpenAsync(ct);
        await _session.Connection.ExecuteAsync(new CommandDefinition(
            $$"""
            INSERT INTO {{_tableName}} (
                "MessageId", "MessageKind", "MessageTypeName", "SourceModule",
                "TargetModule", "DurableOperationId", "TraceParent", "TraceState",
                "TraceBaggage", "CausationId", "PartitionKey", "Payload",
                "Metadata", "CreatedAtUtc", "StoredAtUtc", "Status", "AttemptCount",
                "NextAttemptAtUtc", "DispatchedAtUtc", "FailedAtUtc", "FailureReason",
                "ClaimedBy", "ClaimedUntilUtc"
            )
            VALUES (
                @MessageId, @MessageKind, @MessageTypeName, @SourceModule,
                @TargetModule, @DurableOperationId, @TraceParent, @TraceState,
                @TraceBaggage, @CausationId, @PartitionKey, @Payload,
                @Metadata, @CreatedAtUtc, @StoredAtUtc, @Status, @AttemptCount,
                @NextAttemptAtUtc, @DispatchedAtUtc, @FailedAtUtc, @FailureReason,
                @ClaimedBy, @ClaimedUntilUtc
            )
            """,
            new
            {
                envelope.MessageId,
                MessageKind = envelope.MessageKind.ToString(),
                envelope.MessageTypeName,
                envelope.SourceModule,
                envelope.TargetModule,
                envelope.DurableOperationId,
                TraceParent = envelope.TraceContext?.TraceParent,
                TraceState = envelope.TraceContext?.TraceState,
                TraceBaggage = envelope.TraceContext?.Baggage,
                envelope.CausationId,
                envelope.PartitionKey,
                envelope.Payload,
                envelope.Metadata,
                envelope.CreatedAtUtc,
                StoredAtUtc = storedAtUtc,
                Status = DurableOutboxStatus.Pending.ToString(),
                AttemptCount = 0,
                NextAttemptAtUtc = (DateTimeOffset?)null,
                DispatchedAtUtc = (DateTimeOffset?)null,
                FailedAtUtc = (DateTimeOffset?)null,
                FailureReason = (string?)null,
                ClaimedBy = (string?)null,
                ClaimedUntilUtc = (DateTimeOffset?)null,
            },
            _session.Transaction,
            cancellationToken: ct));
    }
}

public sealed class PostgresModuleDurableOutboxWriter(
    string moduleName,
    IPostgresModuleSession session,
    TimeProvider? timeProvider = null,
    string? schema = null)
    : IDurableModuleOutboxWriter
{
    private readonly PostgresDurableOutboxWriter _writer = new(
        session,
        timeProvider,
        schema);

    public string ModuleName { get; } = moduleName.NormalizeRequired(
        nameof(moduleName),
        "Module name");

    public ValueTask WriteAsync(
        DurableMessageEnvelope envelope,
        CancellationToken ct = default)
    {
        return _writer.WriteAsync(envelope, ct);
    }
}
