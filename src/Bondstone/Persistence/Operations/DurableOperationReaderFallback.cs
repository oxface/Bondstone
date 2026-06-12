using Bondstone.Messaging;

namespace Bondstone.Persistence;

internal sealed class DurableOperationReaderFallback : IDisposable, IAsyncDisposable
{
    private readonly Lazy<IDurableOperationReader?> _reader;
    private readonly bool _ownsReader;
    private bool _disposed;

    public DurableOperationReaderFallback(
        Func<IDurableOperationReader?> createReader,
        bool ownsReader)
    {
        ArgumentNullException.ThrowIfNull(createReader);
        _reader = new Lazy<IDurableOperationReader?>(createReader);
        _ownsReader = ownsReader;
    }

    public IDurableOperationReader? Reader => _reader.Value;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (!_ownsReader || !_reader.IsValueCreated)
        {
            _disposed = true;
            return;
        }

        IDurableOperationReader? reader = _reader.Value;
        if (reader is IDisposable disposable)
        {
            disposable.Dispose();
            _disposed = true;
            return;
        }

        if (reader is IAsyncDisposable)
        {
            throw new InvalidOperationException(
                $"Fallback durable operation reader type '{reader.GetType().FullName}' only implements "
                + $"{nameof(IAsyncDisposable)}. Dispose the scope or service provider asynchronously.");
        }

        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_ownsReader || !_reader.IsValueCreated)
        {
            return;
        }

        IDurableOperationReader? reader = _reader.Value;
        if (reader is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
            return;
        }

        if (reader is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
