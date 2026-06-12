using Bondstone.Messaging;

namespace Bondstone.Persistence;

internal sealed class DurableOperationReaderFallback
{
    private readonly Lazy<IDurableOperationReader?> _reader;

    public DurableOperationReaderFallback(Func<IDurableOperationReader?> createReader)
    {
        ArgumentNullException.ThrowIfNull(createReader);
        _reader = new Lazy<IDurableOperationReader?>(createReader);
    }

    public IDurableOperationReader? Reader => _reader.Value;
}
