using Npgsql;

namespace Bondstone.Persistence.Postgres.Persistence;

public sealed class PostgresModuleSession(NpgsqlDataSource dataSource)
    : IPostgresModuleSession, IAsyncDisposable
{
    private readonly NpgsqlDataSource _dataSource =
        dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    private NpgsqlConnection? _connection;
    private NpgsqlTransaction? _transaction;
    private bool _ownsTransaction;

    public NpgsqlConnection Connection =>
        _connection ?? throw new InvalidOperationException(
            "PostgreSQL module session has not been opened.");

    public NpgsqlTransaction? Transaction => _transaction;

    public async ValueTask EnsureOpenAsync(CancellationToken ct = default)
    {
        if (_connection is not null)
        {
            return;
        }

        _connection = await _dataSource.OpenConnectionAsync(ct);
    }

    public async ValueTask ExecuteInTransactionAsync(
        Func<CancellationToken, ValueTask> operation,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await EnsureOpenAsync(ct);

        if (_transaction is not null)
        {
            await operation(ct);
            return;
        }

        _transaction = await Connection.BeginTransactionAsync(ct);
        _ownsTransaction = true;

        try
        {
            await operation(ct);
            await _transaction.CommitAsync(ct);
        }
        catch
        {
            await _transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
            _ownsTransaction = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            if (_ownsTransaction)
            {
                await _transaction.RollbackAsync(CancellationToken.None);
            }

            await _transaction.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
