using Bondstone.Utility;
using Bondstone.Modules;

namespace Bondstone.Persistence;

internal sealed class DurableModuleInboxHandlerExecutorResolver(
    IEnumerable<IDurableModuleInboxHandlerExecutor> moduleExecutors,
    IDurableInboxHandlerExecutor? fallbackExecutor,
    IBondstoneModuleRegistry moduleRegistry)
{
    private readonly IDurableModuleInboxHandlerExecutor[] _moduleExecutors =
        DurableModulePersistenceRegistrationValidator.ToValidatedArray(
            moduleExecutors,
            static executor => executor.ModuleName,
            "durable module inbox handler executor");
    private readonly IBondstoneModuleRegistry _moduleRegistry =
        moduleRegistry ?? throw new ArgumentNullException(nameof(moduleRegistry));

    public IDurableInboxHandlerExecutor Resolve(string moduleName)
    {
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");

        IDurableModuleInboxHandlerExecutor? moduleExecutor = _moduleExecutors
            .SingleOrDefault(executor => StringComparer.Ordinal.Equals(
                executor.ModuleName.NormalizeRequired(
                    nameof(IDurableModuleInboxHandlerExecutor.ModuleName),
                    "Module name"),
                normalizedModuleName));

        if (moduleExecutor is not null)
        {
            return moduleExecutor;
        }

        if (_moduleExecutors.Length == 0 && fallbackExecutor is not null)
        {
            return fallbackExecutor;
        }

        throw new InvalidOperationException(
            DurableModulePersistenceDiagnosticFormatter.MissingModuleRegistration(
                _moduleRegistry,
                normalizedModuleName,
                "durable module inbox handler executor"));
    }
}
