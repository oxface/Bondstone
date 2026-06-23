using Bondstone.Utility;
using System.ComponentModel;

namespace Bondstone.Persistence;

/// <summary>
/// Registers the provider-side durable incoming inbox ingestion boundary for one module.
/// </summary>
/// <remarks>
/// This type is public for provider and advanced composition packages, but is
/// hidden from normal IntelliSense. Application setup should prefer the module
/// persistence extension methods that create these registrations.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DurableModuleIncomingInboxIngestionBoundaryRegistration
{
    private readonly Func<IServiceProvider, DurableIncomingInboxIngestionBoundary>
        _createBoundary;

    /// <summary>
    /// Initializes the registration for one module incoming inbox ingestion boundary.
    /// </summary>
    /// <param name="moduleName">The module that owns the incoming inbox ingestion boundary.</param>
    /// <param name="createBoundary">The scoped factory that resolves the provider ingestion boundary.</param>
    /// <remarks>
    /// The factory runs inside the current DI scope for the selected module.
    /// It should return a lightweight wrapper over DI-owned scoped services and
    /// should not create owned disposable resources outside DI ownership.
    /// </remarks>
    public DurableModuleIncomingInboxIngestionBoundaryRegistration(
        string moduleName,
        Func<IServiceProvider, DurableIncomingInboxIngestionBoundary> createBoundary)
    {
        ModuleName = moduleName.NormalizeRequired(nameof(moduleName), "Module name");
        _createBoundary = createBoundary ?? throw new ArgumentNullException(nameof(createBoundary));
    }

    /// <summary>
    /// Gets the module that owns the incoming inbox ingestion boundary.
    /// </summary>
    public string ModuleName { get; }

    /// <summary>
    /// Creates the incoming inbox ingestion boundary from the current DI scope.
    /// </summary>
    /// <param name="serviceProvider">The current scoped service provider.</param>
    /// <returns>The provider-side incoming inbox ingestion boundary.</returns>
    public DurableIncomingInboxIngestionBoundary CreateBoundary(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return _createBoundary(serviceProvider)
            ?? throw new InvalidOperationException(
                $"Durable module incoming inbox ingestion boundary factory for module '{ModuleName}' returned null.");
    }
}
