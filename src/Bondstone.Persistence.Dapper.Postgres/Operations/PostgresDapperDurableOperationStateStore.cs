using Bondstone.Messaging;
using Bondstone.Persistence;
using Bondstone.Persistence.Dapper.Postgres.Persistence;
using Bondstone.Utility;
using Dapper;

namespace Bondstone.Persistence.Dapper.Postgres.Operations;

public sealed class PostgresDapperDurableOperationStateStore(
    IPostgresDapperModuleSession session,
    string? schema = null)
    : IDurableOperationStateStore
{
    private readonly IPostgresDapperModuleSession _session =
        session ?? throw new ArgumentNullException(nameof(session));
    private readonly string _tableName = PostgresDapperTableIdentifier.Build(
        PostgresDapperDurableTableNames.OperationStates,
        schema);

    public async ValueTask<DurableOperationState?> GetStateAsync(
        Guid durableOperationId,
        CancellationToken ct = default)
    {
        await _session.EnsureOpenAsync(ct);
        PostgresDapperOperationStateRow? row =
            await _session.Connection.QuerySingleOrDefaultAsync<PostgresDapperOperationStateRow>(
                new CommandDefinition(
                    $$"""
                    SELECT "DurableOperationId", "Status", "UpdatedAtUtc",
                        "ResultPayload", "FailureReason"
                    FROM {{_tableName}}
                    WHERE "DurableOperationId" = @DurableOperationId
                    """,
                    new { DurableOperationId = durableOperationId },
                    _session.Transaction,
                    cancellationToken: ct));

        return row?.ToState();
    }

    public async ValueTask SaveAsync(
        DurableOperationState state,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(state);

        await _session.EnsureOpenAsync(ct);
        await _session.Connection.ExecuteAsync(new CommandDefinition(
            $$"""
            INSERT INTO {{_tableName}} (
                "DurableOperationId", "Status", "UpdatedAtUtc",
                "ResultPayload", "FailureReason"
            )
            VALUES (
                @DurableOperationId, @Status, @UpdatedAtUtc,
                @ResultPayload, @FailureReason
            )
            ON CONFLICT ("DurableOperationId") DO UPDATE
            SET
                "Status" = EXCLUDED."Status",
                "UpdatedAtUtc" = EXCLUDED."UpdatedAtUtc",
                "ResultPayload" = EXCLUDED."ResultPayload",
                "FailureReason" = EXCLUDED."FailureReason"
            """,
            new
            {
                state.DurableOperationId,
                Status = state.Status.ToString(),
                state.UpdatedAtUtc,
                state.ResultPayload,
                state.FailureReason,
            },
            _session.Transaction,
            cancellationToken: ct));
    }
}

public sealed class PostgresDapperModuleDurableOperationStateStore(
    string moduleName,
    IPostgresDapperModuleSession session,
    string? schema = null)
    : IDurableModuleOperationStateStore
{
    private readonly PostgresDapperDurableOperationStateStore _store = new(
        session,
        schema);

    public string ModuleName { get; } = moduleName.NormalizeRequired(
        nameof(moduleName),
        "Module name");

    public ValueTask<DurableOperationState?> GetStateAsync(
        Guid durableOperationId,
        CancellationToken ct = default)
    {
        return _store.GetStateAsync(durableOperationId, ct);
    }

    public ValueTask SaveAsync(
        DurableOperationState state,
        CancellationToken ct = default)
    {
        return _store.SaveAsync(state, ct);
    }
}
