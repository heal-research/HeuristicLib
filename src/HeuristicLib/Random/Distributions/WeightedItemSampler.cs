namespace HEAL.HeuristicLib.Random;

/// <summary>Repeatedly samples an item from a fixed weighted collection.</summary>
/// <remarks>
/// <para>
/// Items and weights are snapshotted at construction. Null or empty weights select uniformly. Nonempty weights use the
/// same permissive semantics as <see cref="WeightedIndexSampler"/> and are compiled once for repeated draws.
/// </para>
/// <para>
/// <see cref="Weights"/> is the only member a <c>with</c> expression may set, so <c>sampler with { Weights = … }</c>
/// reweights the unchanged items. Sampling a different set of items means constructing a new sampler.
/// </para>
/// </remarks>
public sealed record WeightedItemSampler<T> : IDistribution<T>
{
    private readonly WeightedIndexSampler indexSampler;

    public ValueArray<T> Items { get; }

    /// <summary>
    /// Gets the configured weights, exactly as supplied, or an empty collection for uniform sampling. Setting them
    /// reweights <see cref="Items"/> through <see cref="WeightedIndexSampler"/>, which validates and compiles them.
    /// </summary>
    public ValueArray<double> Weights
    {
        get => indexSampler.Weights;
        init => indexSampler = indexSampler with { Weights = value };
    }

    public int Count => Items.Count;

    public WeightedItemSampler(IReadOnlyList<T> items, IReadOnlyList<double>? weights = null)
    {
        if (items.Count == 0)
            throw new ArgumentException("At least one item must be provided.", nameof(items));

        Items = items.ToValueArray();
        indexSampler = new WeightedIndexSampler(Items.Count, weights);
    }

    public T Sample(IRandomNumberGenerator random) => Items[indexSampler.Sample(random)];

    /// <summary>
    /// Compares the configured items and weights. Stating this explicitly instead of relying on the synthesized record
    /// equality keeps equality pinned to the configuration, independent of any derived state the sampler holds.
    /// </summary>
    public bool Equals(WeightedItemSampler<T>? other) =>
        other is not null && Items.Equals(other.Items) && Weights.Equals(other.Weights);

    public override int GetHashCode() => HashCode.Combine(Items, Weights);

    public override string ToString() => $"{Items} with weights {Weights}";
}
