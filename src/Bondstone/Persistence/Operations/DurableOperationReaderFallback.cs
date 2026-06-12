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

        _disposed = true;
        if (!_ownsReader || !_reader.IsValueCreated)
        {
            return;
        }

        if (_reader.Value is IDisposable disposable)
        {
            disposable.Dispose();
        }
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

        if (_reader.Value is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
            return;
        }

        if (_reader.Value is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
