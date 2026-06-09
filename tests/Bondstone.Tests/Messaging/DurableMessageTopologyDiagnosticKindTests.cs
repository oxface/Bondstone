using Bondstone.Messaging;
using Xunit;

namespace Bondstone.Tests.Messaging;

public sealed class DurableMessageTopologyDiagnosticKindTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Values_IncludeCommandAndEventTopologyNames()
    {
        Assert.Equal(1, (int)DurableMessageTopologyDiagnosticKind.CommandRoute);
        Assert.Equal(2, (int)DurableMessageTopologyDiagnosticKind.CommandDestination);
        Assert.Equal(3, (int)DurableMessageTopologyDiagnosticKind.CommandReceiveEndpoint);
        Assert.Equal(4, (int)DurableMessageTopologyDiagnosticKind.EventDestination);
        Assert.Equal(5, (int)DurableMessageTopologyDiagnosticKind.EventSubscription);
    }
}
