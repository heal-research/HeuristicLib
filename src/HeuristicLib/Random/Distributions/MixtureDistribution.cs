namespace HEAL.HeuristicLib.Random.Distributions;

public sealed record MixtureDistribution<T> : IDistribution<T>
{
    public ValueArray<IDistribution<T>> Distributions { get; }
    public ValueArray<double> Weights { get; }

    public MixtureDistribution(ImmutableArray<IDistribution<T>> distributions)
    {
        if (distributions.IsDefaultOrEmpty)
            throw new ArgumentException("A mixture needs at least one distribution.", nameof(distributions));

        Distributions = distributions;
        Weights = ImmutableArray<double>.Empty;
    }

    public MixtureDistribution(ImmutableArray<IDistribution<T>> distributions, ImmutableArray<double> weights)
    {
        if (distributions.IsDefaultOrEmpty)
            throw new ArgumentException("A mixture needs at least one distribution.", nameof(distributions));

        Distributions = distributions;
        Weights = WeightSelection.Normalize(weights, distributions.Length);
    }

    public MixtureDistribution(IEnumerable<(IDistribution<T> Distribution, double Weight)> distributions)
    {
        var entries = distributions.ToArray();
        if (entries.Length == 0)
            throw new ArgumentException("A mixture needs at least one distribution.", nameof(distributions));

        Distributions = entries.Select(entry => entry.Distribution).ToImmutableArray();
        Weights = WeightSelection.Normalize(entries.Select(entry => entry.Weight).ToArray(), entries.Length);
    }

    public T Sample(IRandomNumberGenerator random)
    {
        var distributionIndex = WeightSelection.SelectIndex(random, Distributions.Count, Weights.AsSpan());
        var distribution = Distributions[distributionIndex];
        return distribution.Sample(random);
    }
}
