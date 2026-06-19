namespace Bondstone.Modules;

/// <summary>
/// Reads registered module query routes.
/// </summary>
public interface IModuleQueryRouteRegistry
{
    /// <summary>
    /// Gets all registered query routes.
    /// </summary>
    IReadOnlyCollection<ModuleQueryRoute> Routes { get; }

    /// <summary>
    /// Gets a query route by module and query CLR type.
    /// </summary>
    /// <param name="moduleName">The module that owns the query route.</param>
    /// <param name="queryType">The query CLR type.</param>
    /// <returns>The matching query route.</returns>
    ModuleQueryRoute GetByQueryType(
        string moduleName,
        Type queryType);

    /// <summary>
    /// Attempts to get a query route by module and query CLR type.
    /// </summary>
    /// <param name="moduleName">The module that owns the query route.</param>
    /// <param name="queryType">The query CLR type.</param>
    /// <param name="route">The matching query route when found.</param>
    /// <returns>True when a matching route exists; otherwise false.</returns>
    bool TryGetByQueryType(
        string moduleName,
        Type queryType,
        out ModuleQueryRoute? route);
}
