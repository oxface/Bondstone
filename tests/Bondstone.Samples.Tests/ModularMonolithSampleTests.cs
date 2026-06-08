using Bondstone.Messaging;
using Bondstone.Samples.ModularMonolith;
using Xunit;

namespace Bondstone.Samples.Tests;

public sealed class ModularMonolithSampleTests(PostgreSqlSampleFixture fixture)
    : IClassFixture<PostgreSqlSampleFixture>
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task RunAsync_WhenDurableCommandIsSent_ReceivesThroughModuleEndpoint()
    {
        SampleRunResult result = await ModularMonolithSample.RunAsync(
            fixture.ConnectionString,
            resetDatabase: true,
            timeout: TimeSpan.FromSeconds(20));

        Assert.Equal(1, result.OrderCount);
        Assert.Equal(1, result.ReservationCount);
        Assert.Equal(1, result.ProcessedInboxCount);
        Assert.Equal(1, result.DispatchedOutboxCount);
        Assert.Equal(DurableOperationStatus.Completed, result.OperationStatus);
    }
}
