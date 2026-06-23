using Microsoft.Extensions.Logging;

namespace Bondstone.Hosting.IncomingInbox;

internal static class DurableIncomingInboxWorkerLogEvents
{
    public static readonly EventId ProcessBatchFailed = new(
        2001,
        nameof(ProcessBatchFailed));
}
