using System.Diagnostics;
using Bondstone.Transport.Rebus.Outbox;

namespace Bondstone.Transport.Rebus.Inbox;

internal static class RebusReceiveTelemetry
{
    public static Activity? StartReceiveActivity(
        string activityName,
        RebusDurableMessageEnvelope envelope,
        string handlerIdentity)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        ActivityContext parentContext = default;
        bool hasParentContext = false;

        if (!string.IsNullOrWhiteSpace(envelope.TraceParent))
        {
            if (!ActivityContext.TryParse(envelope.TraceParent, envelope.TraceState, out parentContext))
            {
                throw new ArgumentException(
                    "Trace parent must be a valid W3C traceparent value.",
                    nameof(envelope));
            }

            hasParentContext = true;
        }

        Activity? activity = hasParentContext
            ? BondstoneRebusTelemetry.ActivitySource.StartActivity(
                activityName,
                ActivityKind.Consumer,
                parentContext)
            : BondstoneRebusTelemetry.ActivitySource.StartActivity(
                activityName,
                ActivityKind.Consumer);

        if (activity is null)
        {
            return null;
        }

        activity.SetTag("bondstone.transport", "rebus");
        activity.SetTag("bondstone.message_id", envelope.MessageId.ToString("D"));
        activity.SetTag("bondstone.message_type", envelope.MessageTypeName);
        activity.SetTag("bondstone.source_module", envelope.SourceModule);
        activity.SetTag("bondstone.target_module", envelope.TargetModule);
        activity.SetTag("bondstone.handler_identity", handlerIdentity);

        return activity;
    }
}
