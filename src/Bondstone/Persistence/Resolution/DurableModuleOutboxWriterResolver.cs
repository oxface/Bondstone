using Bondstone.Utility;

namespace Bondstone.Persistence;

internal sealed class DurableModuleOutboxWriterResolver(
    IEnumerable<IDurableModuleOutboxWriter> moduleWriters,
    IDurableOutboxWriter? fallbackWriter)
{
    private readonly IDurableModuleOutboxWriter[] _moduleWriters =
        DurableModulePersistenceRegistrationValidator.ToValidatedArray(
            moduleWriters,
            static writer => writer.ModuleName,
            "durable module outbox writer");

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
            $"No durable outbox writer is registered for module '{normalizedModuleName}'.");
    }
}
