using Bondstone.Modules;
using Xunit;

namespace Bondstone.Tests.Modules;

public sealed class ModulePipelineFeatureCollectionTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void TryGet_WhenFeatureScopeIsActive_ReturnsNearestFeature()
    {
        var features = new ModulePipelineFeatureCollection();
        var outerFeature = new TestFeature("outer");
        var innerFeature = new TestFeature("inner");

        using IDisposable outerScope = features.Push<ITestFeature>(outerFeature);
        Assert.True(features.TryGet(out ITestFeature? activeFeature));
        Assert.Same(outerFeature, activeFeature);

        using (features.Push<ITestFeature>(innerFeature))
        {
            Assert.True(features.TryGet(out activeFeature));
            Assert.Same(innerFeature, activeFeature);
        }

        Assert.True(features.TryGet(out activeFeature));
        Assert.Same(outerFeature, activeFeature);

        outerScope.Dispose();
        Assert.False(features.TryGet<ITestFeature>(out activeFeature));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ScopeDispose_WhenScopesAreDisposedOutOfOrder_Throws()
    {
        var features = new ModulePipelineFeatureCollection();

        using IDisposable outerScope = features.Push<ITestFeature>(
            new TestFeature("outer"));
        using IDisposable innerScope = features.Push<ITestFeature>(
            new TestFeature("inner"));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            outerScope.Dispose);

        Assert.Contains("disposed in reverse order", exception.Message, StringComparison.Ordinal);

        innerScope.Dispose();
        outerScope.Dispose();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ScopeDispose_WhenDifferentFeatureTypesAreDisposedOutOfOrder_Throws()
    {
        var features = new ModulePipelineFeatureCollection();

        using IDisposable outerScope = features.Push<ITestFeature>(
            new TestFeature("outer"));
        using IDisposable innerScope = features.Push<IAnotherFeature>(
            new AnotherFeature("inner"));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            outerScope.Dispose);

        Assert.Contains("disposed in reverse order", exception.Message, StringComparison.Ordinal);

        innerScope.Dispose();
        outerScope.Dispose();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TryGet_WhenFeatureWasPushedWithConcreteType_DoesNotResolveInterfaceType()
    {
        var features = new ModulePipelineFeatureCollection();
        var feature = new TestFeature("concrete");

        using IDisposable scope = features.Push(feature);

        Assert.True(features.TryGet(out TestFeature? concreteFeature));
        Assert.Same(feature, concreteFeature);
        Assert.False(features.TryGet<ITestFeature>(out _));
    }

    private interface ITestFeature
    {
        string Name { get; }
    }

    private interface IAnotherFeature
    {
        string Name { get; }
    }

    private sealed record TestFeature(string Name) : ITestFeature;

    private sealed record AnotherFeature(string Name) : IAnotherFeature;
}
