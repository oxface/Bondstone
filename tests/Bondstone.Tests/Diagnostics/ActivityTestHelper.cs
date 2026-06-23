using System.Diagnostics;

namespace Bondstone.Tests;

internal static class ActivityTestHelper
{
    public static ActivityListener CreateActivityListener(
        string sourceName,
        IList<Activity> stoppedActivities)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == sourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllData,
            ActivityStopped = stoppedActivities.Add,
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    public static object? GetTag(
        Activity activity,
        string name)
    {
        return activity.TagObjects.SingleOrDefault(tag => tag.Key == name).Value;
    }
}
