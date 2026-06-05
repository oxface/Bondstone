using Bondstone.Hosting.Outbox;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bondstone.Hosting.Tests.Outbox;

public sealed class DurableOutboxWorkerOptionsTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_WhenWorkerIdIsBlank_Throws()
    {
        var options = new DurableOutboxWorkerOptions { WorkerId = " " };

        ArgumentException exception = Assert.Throws<ArgumentException>(options.Validate);

        Assert.Equal(nameof(DurableOutboxWorkerOptions.WorkerId), exception.ParamName);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenBatchSizeIsNotPositive_Throws(int batchSize)
    {
        var options = new DurableOutboxWorkerOptions { BatchSize = batchSize };

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(options.Validate);

        Assert.Equal(nameof(DurableOutboxWorkerOptions.BatchSize), exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_WhenValuesAreValid_NormalizesWorkerId()
    {
        var options = new DurableOutboxWorkerOptions
        {
            WorkerId = " worker-1 ",
            LeaseDuration = TimeSpan.FromMinutes(5),
            BatchSize = 10,
            PollingInterval = TimeSpan.FromSeconds(1),
            FailureDelay = TimeSpan.FromSeconds(5),
        };

        options.Validate();

        Assert.Equal("worker-1", options.WorkerId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void OptionsValidator_WhenOptionsAreInvalid_ReturnsFailure()
    {
        var validator = new DurableOutboxWorkerOptionsValidator();

        ValidateOptionsResult result = validator.Validate(
            Options.DefaultName,
            new DurableOutboxWorkerOptions { LeaseDuration = TimeSpan.Zero });

        Assert.True(result.Failed);
    }
}
