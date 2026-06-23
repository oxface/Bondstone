using Bondstone.Hosting.IncomingInbox;
using Bondstone.Persistence;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bondstone.Hosting.Tests.IncomingInbox;

public sealed class DurableIncomingInboxWorkerOptionsTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Defaults_UseDurableIncomingInboxPolicyDefaults()
    {
        var options = new DurableIncomingInboxWorkerOptions();

        Assert.False(string.IsNullOrWhiteSpace(options.WorkerId));
        Assert.Equal(TimeSpan.FromMinutes(5), options.LeaseDuration);
        Assert.Equal(100, options.BatchSize);
        Assert.Equal(TimeSpan.FromSeconds(1), options.PollingInterval);
        Assert.Equal(TimeSpan.FromSeconds(5), options.FailureDelay);
        Assert.Equal(DurableIncomingInboxProcessingOptions.DefaultMaxAttempts, options.MaxAttempts);
        Assert.Equal(
            DurableIncomingInboxProcessingOptions.DefaultRetryDelays,
            options.RetryDelays);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_WhenWorkerIdIsBlank_Throws()
    {
        var options = new DurableIncomingInboxWorkerOptions { WorkerId = " " };

        ArgumentException exception = Assert.Throws<ArgumentException>(options.Validate);

        Assert.Equal(nameof(DurableIncomingInboxWorkerOptions.WorkerId), exception.ParamName);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenBatchSizeIsNotPositive_Throws(int batchSize)
    {
        var options = new DurableIncomingInboxWorkerOptions { BatchSize = batchSize };

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(options.Validate);

        Assert.Equal(nameof(DurableIncomingInboxWorkerOptions.BatchSize), exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_WhenRetryDelayIsNegative_Throws()
    {
        var options = new DurableIncomingInboxWorkerOptions
        {
            RetryDelays = [TimeSpan.Zero, TimeSpan.FromSeconds(-1)],
        };

        ArgumentException exception = Assert.Throws<ArgumentException>(options.Validate);

        Assert.Equal(nameof(DurableIncomingInboxWorkerOptions.RetryDelays), exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CreateProcessingOptions_WhenValuesAreValid_UsesRetryPolicyKnobs()
    {
        var options = new DurableIncomingInboxWorkerOptions
        {
            WorkerId = " incoming-worker ",
            MaxAttempts = 9,
            RetryDelays = [TimeSpan.Zero, TimeSpan.FromSeconds(2)],
        };

        DurableIncomingInboxProcessingOptions processingOptions = options.CreateProcessingOptions();

        Assert.Equal("incoming-worker", options.WorkerId);
        Assert.Equal(9, processingOptions.MaxAttempts);
        Assert.Equal(
            [TimeSpan.Zero, TimeSpan.FromSeconds(2)],
            processingOptions.RetryDelays);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void OptionsValidator_WhenOptionsAreInvalid_ReturnsFailure()
    {
        var validator = new DurableIncomingInboxWorkerOptionsValidator();

        ValidateOptionsResult result = validator.Validate(
            Options.DefaultName,
            new DurableIncomingInboxWorkerOptions { LeaseDuration = TimeSpan.Zero });

        Assert.True(result.Failed);
    }
}
