using Bondstone.Utility;

namespace Bondstone.Persistence;

public sealed class DurableModuleOutboxWriterRegistration
{
    private readonly Func<IServiceProvider, IDurableOutboxWriter> _createWriter;

    public DurableModuleOutboxWriterRegistration(
        string moduleName,
        Func<IServiceProvider, IDurableOutboxWriter> createWriter)
    {
        ModuleName = moduleName.NormalizeRequired(nameof(moduleName), "Module name");
        _createWriter = createWriter ?? throw new ArgumentNullException(nameof(createWriter));
    }

    public string ModuleName { get; }

    internal IDurableOutboxWriter CreateWriter(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return _createWriter(serviceProvider)
            ?? throw new InvalidOperationException(
                $"Durable module outbox writer factory for module '{ModuleName}' returned null.");
    }
}
