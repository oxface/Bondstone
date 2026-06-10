using Bondstone.Utility;
using Bondstone.Modules;

namespace Bondstone.Persistence;

internal sealed class DurableModuleOutboxWriterResolver(
    IEnumerable<IDurableModuleOutboxWriter> moduleWriters,
    IDurableOutboxWriter? fallbackWriter,
    IBondstoneModuleRegistry moduleRegistry)
{
    private readonly IDurableModuleOutboxWriter[] _moduleWriters =
        DurableModulePersistenceRegistrationValidator.ToValidatedArray(
            moduleWriters,
            static writer => writer.ModuleName,
            "durable module outbox writer");
    private readonly IBondstoneModuleRegistry _moduleRegistry =
        moduleRegistry ?? throw new ArgumentNullException(nameof(moduleRegistry));

    public IDurableOutboxWriter Resolve(string moduleName)
    {
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");

        IDurableModuleOutboxWriter? moduleWriter = _moduleWriters
            .SingleOrDefault(writer => StringComparer.Ordinal.Equals(
                writer.ModuleName.NormalizeRequired(
                    nameof(IDurableModuleOutboxWriter.ModuleName),
                    "Module name"),
                normalizedModuleName));

        if (moduleWriter is not null)
        {
            return moduleWriter;
        }

        if (_moduleWriters.Length == 0 && fallbackWriter is not null)
        {
            return fallbackWriter;
        }

        throw new InvalidOperationException(
            DurableModulePersistenceDiagnosticFormatter.MissingModuleRegistration(
                _moduleRegistry,
                normalizedModuleName,
                "durable module outbox writer"));
    }
}
