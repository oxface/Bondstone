using Bondstone.Messaging;

namespace Bondstone.Modules;

/// <summary>
/// Executes registered module queries through Bondstone's immediate read-only query boundary.
/// </summary>
public interface IModuleQueryExecutor
{
    /// <summary>
    /// Executes a query in the named module.
    /// </summary>
    /// <typeparam name="TResult">The result type produced by the query handler.</typeparam>
    /// <param name="moduleName">The target module name.</param>
    /// <param name="query">The query instance.</param>
    /// <param name="ct">A cancellation token for query execution.</param>
    /// <returns>The result produced by the query handler.</returns>
    ValueTask<TResult> ExecuteAsync<TResult>(
        string moduleName,
        IQuery<TResult> query,
        CancellationToken ct = default);
}
