using Bondstone.Utility;

namespace Bondstone.Persistence;

internal sealed class DurableModuleInboxHandlerExecutorResolver(
    IEnumerable<IDurableModuleInboxHandlerExecutor> moduleExecutors,
    IDurableInboxHandlerExecutor? fallbackExecutor)
{
    private readonly IDurableModuleInboxHandlerExecutor[] _moduleExecutors =
        DurableModulePersistenceRegistrationValidator.ToValidatedArray(
            moduleExecutors,
            static executor => executor.ModuleName,
            "durable module inbox handler executor");

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
            $"No durable inbox handler executor is registered for module '{normalizedModuleName}'.");
    }
}
