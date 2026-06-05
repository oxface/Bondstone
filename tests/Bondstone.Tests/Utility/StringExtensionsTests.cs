using Bondstone.Utility;
using Xunit;

namespace Bondstone.Tests.Utility;

public sealed class StringExtensionsTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void NormalizeOptional_WhenValueHasContent_TrimsValue()
    {
        Assert.Equal("sales", "  sales  ".NormalizeOptional());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void NormalizeOptional_WhenValueIsBlank_ReturnsNull()
    {
        Assert.Null(" ".NormalizeOptional());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void NormalizeRequired_WhenValueHasContent_TrimsValue()
    {
        Assert.Equal("sales", "  sales  ".NormalizeRequired("moduleName", "Module name"));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void NormalizeRequired_WhenValueIsBlank_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => " ".NormalizeRequired("moduleName", "Module name"));

        Assert.Equal("moduleName", exception.ParamName);
        Assert.Contains("Module name is required.", exception.Message);
    }
}
