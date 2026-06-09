using System.Diagnostics;

namespace Bondstone.Transport.Rebus.Inbox;

public static class BondstoneRebusTelemetry
{
    public const string ActivitySourceName = "Bondstone.Transport.Rebus";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
