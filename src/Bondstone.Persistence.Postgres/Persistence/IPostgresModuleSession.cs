using Npgsql;

namespace Bondstone.Persistence.Postgres.Persistence;

public interface IPostgresModuleSession
{
    NpgsqlConnection Connection { get; }

    NpgsqlTransaction? Transaction { get; }

    ValueTask EnsureOpenAsync(CancellationToken ct = default);

    ValueTask ExecuteInTransactionAsync(
        Func<CancellationToken, ValueTask> operation,
        CancellationToken ct = default);
}
