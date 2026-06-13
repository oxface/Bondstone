namespace Bondstone.Modules;

public sealed class ModulePipelineFeatureCollection
{
    private readonly Dictionary<Type, List<object>> _features = [];
    private readonly List<FeatureEntry> _scopeStack = [];

    /// <summary>
    /// Pushes a feature for the current pipeline execution.
    /// </summary>
    /// <remarks>
    /// Features are stored under the exact <typeparamref name="TFeature"/> used
    /// when pushing. Consumers should read the feature with the same contract
    /// type, usually a provider-neutral interface. Feature scopes must be
    /// disposed in reverse order across the whole collection.
    /// </remarks>
    public IDisposable Push<TFeature>(TFeature feature)
        where TFeature : class
    {
        ArgumentNullException.ThrowIfNull(feature);

        Type featureType = typeof(TFeature);
        if (!_features.TryGetValue(featureType, out List<object>? features))
        {
            features = [];
            _features.Add(featureType, features);
        }

        features.Add(feature);
        _scopeStack.Add(new FeatureEntry(featureType, feature));
        return new FeatureScope(this, featureType, feature);
    }

    /// <summary>
    /// Gets the nearest active feature stored for the exact
    /// <typeparamref name="TFeature"/> contract type.
    /// </summary>
    public bool TryGet<TFeature>(out TFeature? feature)
        where TFeature : class
    {
        if (_features.TryGetValue(typeof(TFeature), out List<object>? features)
            && features.Count > 0)
        {
            feature = (TFeature)features[^1];
            return true;
        }

        feature = null;
        return false;
    }

    private void Pop(
        Type featureType,
        object feature)
    {
        if (_scopeStack.Count == 0)
        {
            throw new InvalidOperationException(
                $"Module pipeline feature '{featureType.FullName}' scopes must be disposed in reverse order.");
        }

        FeatureEntry activeScope = _scopeStack[^1];
        if (activeScope.FeatureType != featureType
            || !ReferenceEquals(activeScope.Feature, feature))
        {
            throw new InvalidOperationException(
                $"Module pipeline feature '{featureType.FullName}' scopes must be disposed in reverse order.");
        }

        if (!_features.TryGetValue(featureType, out List<object>? features)
            || features.Count == 0
            || !ReferenceEquals(features[^1], feature))
        {
            throw new InvalidOperationException(
                $"Module pipeline feature '{featureType.FullName}' scopes must be disposed in reverse order.");
        }

        _scopeStack.RemoveAt(_scopeStack.Count - 1);
        features.RemoveAt(features.Count - 1);
        if (features.Count == 0)
        {
            _features.Remove(featureType);
        }
    }

    private sealed record FeatureEntry(
        Type FeatureType,
        object Feature);

    private sealed class FeatureScope(
        ModulePipelineFeatureCollection collection,
        Type featureType,
        object feature)
        : IDisposable
    {
        private ModulePipelineFeatureCollection? _collection = collection;

        public void Dispose()
        {
            ModulePipelineFeatureCollection? collection = _collection;
            if (collection is null)
            {
                return;
            }

            collection.Pop(featureType, feature);
            _collection = null;
        }
    }
}
