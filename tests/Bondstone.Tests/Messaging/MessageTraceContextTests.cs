using System.Diagnostics;
using Bondstone.Messaging;
using Xunit;

namespace Bondstone.Tests.Messaging;

public sealed class MessageTraceContextTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_NormalizesTraceMetadata()
    {
        var traceContext = new MessageTraceContext(
            "  00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00  ",
            "  state=value  ",
            "  tenant=sales  ");

        Assert.Equal("00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00", traceContext.TraceParent);
        Assert.Equal("state=value", traceContext.TraceState);
        Assert.Equal("tenant=sales", traceContext.Baggage);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenTraceParentIsEmpty_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new MessageTraceContext(" "));

        Assert.Equal("traceParent", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CaptureCurrent_WhenActivityExists_CapturesTraceParentAndBaggage()
    {
        using var activitySource = new ActivitySource("Bondstone.Tests");
        using ActivityListener listener = CreateActivityListener(activitySource.Name);
        using Activity? activity = activitySource.StartActivity("test");

        Assert.NotNull(activity);
        activity.SetBaggage("tenant", "sales");

        MessageTraceContext? traceContext = MessageTraceContext.CaptureCurrent();

        Assert.NotNull(traceContext);
        Assert.Equal(activity.Id, traceContext.TraceParent);
        Assert.Equal("tenant=sales", traceContext.Baggage);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CaptureCurrent_WhenNoActivityExists_ReturnsNull()
    {
        Activity? original = Activity.Current;
        Activity.Current = null;
        try
        {
            Assert.Null(MessageTraceContext.CaptureCurrent());
        }
        finally
        {
            Activity.Current = original;
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TryGetW3CTraceId_WhenTraceParentIsValid_ReturnsTraceId()
    {
        var traceContext = new MessageTraceContext(
            "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00");

        bool parsed = traceContext.TryGetW3CTraceId(out string? traceId);

        Assert.True(parsed);
        Assert.Equal("4bf92f3577b34da6a3ce929d0e0e4736", traceId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TryGetW3CTraceId_WhenTraceParentIsInvalid_ReturnsFalse()
    {
        var traceContext = new MessageTraceContext("not-a-traceparent");

        bool parsed = traceContext.TryGetW3CTraceId(out string? traceId);

        Assert.False(parsed);
        Assert.Null(traceId);
    }

    private static ActivityListener CreateActivityListener(string sourceName)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == sourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}
