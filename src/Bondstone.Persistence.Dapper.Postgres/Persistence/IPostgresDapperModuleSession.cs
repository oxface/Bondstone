using Npgsql;

namespace Bondstone.Persistence.Dapper.Postgres.Persistence;

public interface IPostgresDapperModuleSession
{
    NpgsqlConnection Connection { get; }

    NpgsqlTransaction? Transaction { get; }

    ValueTask EnsureOpenAsync(CancellationToken ct = default);

    ValueTask ExecuteInTransactionAsync(
        Func<CancellationToken, ValueTask> operation,
        CancellationToken ct = default);
}
