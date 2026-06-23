namespace Bondstone.Modules;

/// <summary>
/// Defines a Bondstone module that owns command, event, and persistence registration.
/// </summary>
public interface IBondstoneModule
{
    /// <summary>
    /// Gets the stable module name used for routing, persistence ownership, and diagnostics.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Configures this module's Bondstone registrations.
    /// </summary>
    /// <param name="module">The module builder scoped to this module name.</param>
    void Configure(BondstoneModuleBuilder module);
}
