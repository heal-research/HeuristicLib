namespace HEAL.HeuristicLib.Random.Distributions;

/// <remarks>
/// <see cref="Weights"/> is the only member a <c>with</c> expression may set, so <c>mixture with { Weights = … }</c>
/// reweights the unchanged components. Mixing a different set of distributions means constructing a new mixture.
/// </remarks>
public sealed record MixtureDistribution<T> : IDistribution<T>
{
    private readonly WeightedItemSampler<IDistribution<T>> sampler;

    public ValueArray<IDistribution<T>> Distributions => sampler.Items;

    /// <summary>
    /// Gets the configured weights, exactly as supplied, or an empty collection for uniform selection. Setting them
    /// reweights <see cref="Distributions"/>; an empty collection restores uniform selection.
    /// </summary>
    public ValueArray<double> Weights
    {
        get => sampler.Weights;
        init => sampler = sampler with { Weights = value };
    }

    public MixtureDistribution(IReadOnlyList<IDistribution<T>> distributions, IReadOnlyList<double>? weights = null)
    {
        if (distributions.Count == 0)
            throw new ArgumentException("A mixture needs at least one distribution.", nameof(distributions));

        sampler = new WeightedItemSampler<IDistribution<T>>(distributions, weights);
    }

    public MixtureDistribution(params IEnumerable<(IDistribution<T> Distribution, double Weight)> distributions)
    {
        var entries = distributions.ToArray();
        if (entries.Length == 0)
            throw new ArgumentException("A mixture needs at least one distribution.", nameof(distributions));

        sampler = new WeightedItemSampler<IDistribution<T>>(
            entries.Select(entry => entry.Distribution).ToImmutableArray(),
            entries.Select(entry => entry.Weight).ToImmutableArray());
    }

    public T Sample(IRandomNumberGenerator random) =>
        sampler.Sample(random).Sample(random);
}
