using System.Diagnostics;
using Bondstone.Utility;

namespace Bondstone.Messaging;

public sealed record MessageTraceContext
{
    public MessageTraceContext(
        string traceParent,
        string? traceState = null,
        string? baggage = null)
    {
        TraceParent = traceParent.NormalizeRequired(nameof(traceParent), "Trace parent");
        TraceState = traceState.NormalizeOptional();
        Baggage = baggage.NormalizeOptional();
    }

    public string TraceParent { get; }

    public string? TraceState { get; }

    public string? Baggage { get; }

    public static MessageTraceContext? CaptureCurrent()
    {
        Activity? activity = Activity.Current;
        if (activity?.Id is null)
        {
            return null;
        }

        return new MessageTraceContext(
            activity.Id,
            activity.TraceStateString,
            CaptureBaggage(activity));
    }

    private static string? CaptureBaggage(Activity activity)
    {
        string[] baggage = activity.Baggage
            .Select(static item => string.IsNullOrEmpty(item.Value)
                ? Uri.EscapeDataString(item.Key)
                : $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value)}")
            .ToArray();

        return baggage.Length == 0
            ? null
            : string.Join(",", baggage);
    }
}
