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
            string messageDescription = messageKind == MessageKind.Command
                ? $"command route for {routeDescription}"
                : $"event route for {routeDescription}";
            throw new InvalidOperationException(
                $"No durable outbox transport route is configured for {messageDescription}. Checked transports: {transportNames}.");
        }

        throw new InvalidOperationException(
            $"Multiple durable outbox transport routes are configured for {routeDescription}: {FormatTransportNames(matches)}. Configure exactly one transport route for this durable message.");
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
}
