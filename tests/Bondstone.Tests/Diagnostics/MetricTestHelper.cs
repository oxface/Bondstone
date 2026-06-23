using System.Diagnostics.Metrics;

namespace Bondstone.Tests;

internal static class MetricTestHelper
{
    public static MeterListener CreateMeterListener(
        string meterName,
        IList<MetricMeasurement> measurements)
    {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == meterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>(
            (instrument, measurement, tags, _) =>
            {
                measurements.Add(new MetricMeasurement(
                    instrument.Name,
                    measurement,
                    tags.ToArray()));
            });
        listener.Start();
        return listener;
    }

    public static IReadOnlyList<MetricMeasurement> FindMeasurements(
        IEnumerable<MetricMeasurement> measurements,
        string instrumentName,
        string tagName,
        object tagValue)
    {
        return measurements
            .Where(measurement =>
                measurement.InstrumentName == instrumentName
                && measurement.HasTag(tagName, tagValue))
            .ToArray();
    }
}

internal sealed record MetricMeasurement(
    string InstrumentName,
    long Value,
    IReadOnlyList<KeyValuePair<string, object?>> Tags)
{
    public bool HasTag(
        string name,
        object? value)
    {
        return Tags.Any(tag =>
            tag.Key == name
            && Equals(tag.Value, value));
    }

    public object? GetTag(
        string name)
    {
        return Tags.SingleOrDefault(tag => tag.Key == name).Value;
    }
}
