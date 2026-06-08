using Bondstone.Utility;

namespace Bondstone.Persistence;

internal sealed class DurableModuleInboxHandlerExecutorResolver(
    IEnumerable<IDurableModuleInboxHandlerExecutor> moduleExecutors,
    IDurableInboxHandlerExecutor? fallbackExecutor)
{
    private readonly IDurableModuleInboxHandlerExecutor[] _moduleExecutors =
        moduleExecutors?.ToArray() ?? throw new ArgumentNullException(nameof(moduleExecutors));

    public IDurableInboxHandlerExecutor Resolve(string moduleName)
    {
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");

        IDurableModuleInboxHandlerExecutor? moduleExecutor = _moduleExecutors
            .SingleOrDefault(executor => StringComparer.Ordinal.Equals(
                executor.ModuleName,
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
