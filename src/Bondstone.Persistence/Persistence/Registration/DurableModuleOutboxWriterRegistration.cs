using Bondstone.Utility;
using System.ComponentModel;

namespace Bondstone.Persistence;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DurableModuleOutboxWriterRegistration
{
    private readonly Func<IServiceProvider, IDurableOutboxWriter> _createWriter;

    /// <remarks>
    /// The factory runs inside the current DI scope for the selected module.
    /// It should return a lightweight wrapper over DI-owned scoped services and
    /// should not create owned disposable resources outside DI ownership.
    /// </remarks>
    public DurableModuleOutboxWriterRegistration(
        string moduleName,
        Func<IServiceProvider, IDurableOutboxWriter> createWriter)
    {
        ModuleName = moduleName.NormalizeRequired(nameof(moduleName), "Module name");
        _createWriter = createWriter ?? throw new ArgumentNullException(nameof(createWriter));
    }

    public string ModuleName { get; }

    public IDurableOutboxWriter CreateWriter(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return _createWriter(serviceProvider)
            ?? throw new InvalidOperationException(
                $"Durable module outbox writer factory for module '{ModuleName}' returned null.");
    }
}
