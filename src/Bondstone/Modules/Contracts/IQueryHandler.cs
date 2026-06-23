using Bondstone.Messaging;

namespace Bondstone.Modules;

/// <summary>
/// Handles an immediate read-only module query.
/// </summary>
/// <typeparam name="TQuery">The query type handled by this handler.</typeparam>
/// <typeparam name="TResult">The result type produced by the handler.</typeparam>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    /// <summary>
    /// Handles the query and returns the typed read result.
    /// </summary>
    /// <param name="query">The query instance.</param>
    /// <param name="ct">A cancellation token for the handling operation.</param>
    /// <returns>The query result.</returns>
    ValueTask<TResult> HandleAsync(
        TQuery query,
        CancellationToken ct = default);
}
