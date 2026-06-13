using Bondstone.Utility;

namespace Bondstone.Persistence.Postgres.Persistence;

internal sealed class PostgresModuleTransactionGuard
{
    private string? _activeModuleName;
    private int _activeDepth;

    public IDisposable Enter(string moduleName)
    {
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");

        if (_activeModuleName is not null
            && !StringComparer.Ordinal.Equals(_activeModuleName, normalizedModuleName))
        {
            throw new InvalidOperationException(
                $"PostgreSQL module transaction for module '{_activeModuleName}' is already active in this service scope. "
                + $"Nested execution for module '{normalizedModuleName}' would share the same PostgreSQL session transaction. "
                + "Execute the nested module work in a separate service scope or split it through durable messaging.");
        }

        _activeModuleName ??= normalizedModuleName;
        _activeDepth++;
        return new Scope(this);
    }

    private void Exit()
    {
        _activeDepth--;
        if (_activeDepth == 0)
        {
            _activeModuleName = null;
        }
    }

    private sealed class Scope(PostgresModuleTransactionGuard guard) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            guard.Exit();
            _disposed = true;
        }
    }
}
