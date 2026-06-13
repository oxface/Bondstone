using System.Diagnostics;
using Bondstone.Messaging;

namespace Bondstone.Modules;

internal static class ModuleReceiveTelemetry
{
    public static readonly ActivitySource ActivitySource = new("Bondstone.Modules");

    public static Activity? StartReceiveActivity(
        string activityName,
        DurableMessageEnvelope envelope,
        string handlerIdentity)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        ActivityContext parentContext = default;
        bool hasParentContext = false;

        MessageTraceContext? traceContext = envelope.TraceContext;
        if (!string.IsNullOrWhiteSpace(traceContext?.TraceParent))
        {
            if (!ActivityContext.TryParse(
                traceContext.TraceParent,
                traceContext.TraceState,
                out parentContext))
            {
                throw new ArgumentException(
                    "Trace parent must be a valid W3C traceparent value.",
                    nameof(envelope));
            }

            hasParentContext = true;
        }

        Activity? activity = hasParentContext
            ? ActivitySource.StartActivity(
                activityName,
                ActivityKind.Consumer,
                parentContext)
            : ActivitySource.StartActivity(
                activityName,
                ActivityKind.Consumer);

        if (activity is null)
        {
            return null;
        }

        activity.SetTag("bondstone.message_id", envelope.MessageId.ToString("D"));
        activity.SetTag("bondstone.message_kind", envelope.MessageKind.ToString());
        activity.SetTag("bondstone.message_type", envelope.MessageTypeName);
        activity.SetTag("bondstone.source_module", envelope.SourceModule);
        activity.SetTag("bondstone.target_module", envelope.TargetModule);
        activity.SetTag("bondstone.handler_identity", handlerIdentity);

        return activity;
    }
}
