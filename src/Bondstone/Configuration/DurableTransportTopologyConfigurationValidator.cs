using Bondstone.Messaging;
using Bondstone.Modules;

namespace Bondstone.Configuration;

internal sealed class DurableTransportTopologyConfigurationValidator(
    IEnumerable<IDurableTransportTopologyDiagnosticSource> diagnosticSources)
    : IBondstoneConfigurationValidator
{
    private readonly IEnumerable<IDurableTransportTopologyDiagnosticSource> _diagnosticSources =
        diagnosticSources ?? throw new ArgumentNullException(nameof(diagnosticSources));

    public void Validate(BondstoneConfigurationValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IDurableTransportTopologyDiagnosticSource[] sources =
            _diagnosticSources.ToArray();
        if (sources.Length == 0)
        {
            return;
        }

        foreach (string moduleName in context.DurableCommandRoutes
            .Select(static route => route.ModuleName)
            .Distinct(StringComparer.Ordinal))
        {
            DurableTransportTopologyRouteDiagnostic[] diagnostics = sources
                .Select(source => source.DescribeCommandRoute(moduleName))
                .ToArray();
            ValidateRouteOwnership(
                MessageKind.Command,
                $"module '{moduleName}'",
                diagnostics);
        }

        foreach (ModulePublishedEventRegistration publishedEvent in context.PublishedEvents)
        {
            DurableTransportTopologyRouteDiagnostic[] diagnostics = sources
                .Select(source => source.DescribeEventRoute(publishedEvent.MessageTypeName))
                .ToArray();
            ValidateRouteOwnership(
                MessageKind.Event,
                $"published event '{publishedEvent.MessageTypeName}' from module '{publishedEvent.ModuleName}'",
                diagnostics);
        }
    }

    private static void ValidateRouteOwnership(
        MessageKind messageKind,
        string routeDescription,
        IReadOnlyCollection<DurableTransportTopologyRouteDiagnostic> diagnostics)
    {
        DurableTransportTopologyRouteDiagnostic[] matches = diagnostics
            .Where(static diagnostic => diagnostic.HasRoute)
            .ToArray();

        if (matches.Length == 1)
        {
            return;
        }

        if (matches.Length == 0)
        {
            string transportNames = FormatTransportNames(diagnostics);
            string failureReasons = FormatFailureReasons(diagnostics);
            string messageDescription = messageKind == MessageKind.Command
                ? $"command route for {routeDescription}"
                : $"event route for {routeDescription}";
            throw new InvalidOperationException(
                $"No durable envelope dispatch route is configured for {messageDescription}. Checked adapters: {transportNames}.{failureReasons}");
        }

        throw new InvalidOperationException(
            $"Multiple durable envelope dispatch routes are configured for {routeDescription}: {FormatTransportNames(matches)}. Configure exactly one adapter route for this durable message.");
    }

    private static string FormatTransportNames(
        IEnumerable<DurableTransportTopologyRouteDiagnostic> diagnostics)
    {
        string[] transportNames = diagnostics
            .Select(static diagnostic => diagnostic.TransportName)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static transportName => transportName, StringComparer.Ordinal)
            .ToArray();

        return transportNames.Length == 0
            ? "(none)"
            : $"'{string.Join("', '", transportNames)}'";
    }

    private static string FormatFailureReasons(
        IEnumerable<DurableTransportTopologyRouteDiagnostic> diagnostics)
    {
        string[] failureReasons = diagnostics
            .Where(static diagnostic => !diagnostic.HasRoute
                && !string.IsNullOrWhiteSpace(diagnostic.FailureReason))
            .OrderBy(static diagnostic => diagnostic.TransportName, StringComparer.Ordinal)
            .Select(static diagnostic =>
                $"{diagnostic.TransportName}: {diagnostic.FailureReason}")
            .ToArray();

        return failureReasons.Length == 0
            ? string.Empty
            : $" Details: {string.Join(" ", failureReasons)}";
    }
}
