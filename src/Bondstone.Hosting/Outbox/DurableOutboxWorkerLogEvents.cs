using Microsoft.Extensions.Logging;

namespace Bondstone.Hosting.Outbox;

internal static class DurableOutboxWorkerLogEvents
{
    public static readonly EventId DispatchBatchFailed = new(
        1001,
        nameof(DispatchBatchFailed));
}
