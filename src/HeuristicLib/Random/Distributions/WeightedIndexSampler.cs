namespace HEAL.HeuristicLib.Random;

/// <summary>
/// Repeatedly samples an index from a weighted set of entries.
/// </summary>
/// <remarks>
/// <para>
/// The sampled entry count is fixed at construction. Null or empty weights select uniformly, while nonempty weights
/// must contain one value per entry. Configured weights are retained exactly as supplied and compiled once into an
/// internal sampling representation. Weight values are never rejected or normalized away, so a uniform sampler,
/// <c>[1, 1]</c> and <c>[∞, ∞]</c> all sample uniformly but remain distinguishable configurations.
/// </para>
/// <para>
/// Positive finite weights participate proportionally. Zero, negative finite weights, negative infinity and
/// <see cref="double.NaN"/> mean never. Positive infinity overrides all finite weights, and several infinite entries
/// share the sampling uniformly. When no entry is selectable, sampling falls back to uniform across all entries.
/// </para>
/// <para>
/// <see cref="Weights"/> is the only member a <c>with</c> expression may set, so <c>sampler with { Weights = … }</c>
/// recompiles the sampling representation for the unchanged entry count. Everything the compiled representation
/// depends on besides the weights must stay constructor-only, because a copied sampler keeps the compiled
/// representation of the original until the <see cref="Weights"/> accessor replaces it.
/// </para>
/// <para>
/// The <see cref="Weights"/> accessor is the single place weight counts are validated. Every type that samples through
/// this one reaches it, whether it constructs a sampler or reweights an existing one, so those types state their
/// weights without repeating the check.
/// </para>
/// </remarks>
public sealed record WeightedIndexSampler : IDistribution<int>
{
    /// <summary>Gets the fixed number of sampled entries.</summary>
    public int Count { get; }

    /// <summary>
    /// Gets the configured weights, exactly as supplied, or an empty collection for uniform sampling. Setting them
    /// compiles the sampling representation once, so construction and <c>with</c> share one code path.
    /// </summary>
    public ValueArray<double> Weights
    {
        get;
        init
        {
            if (!value.IsEmpty && value.Count != Count)
                throw new ArgumentException("Weights must have one entry for each sampled entry.", nameof(value));

            field = value;
            CompileWeights(value, out totalWeight, out cumulativeWeights, out selectableIndices);
        }
    }

    private readonly double totalWeight;
    private readonly double[] cumulativeWeights = [];
    private readonly int[] selectableIndices = [];

    /// <summary>
    /// Constructs a sampler over <paramref name="count"/> entries, using uniform selection when
    /// <paramref name="weights"/> is null or empty.
    /// </summary>
    public WeightedIndexSampler(int count, IReadOnlyList<double>? weights = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        // Count must be assigned first, because the Weights accessor validates against it.
        Count = count;
        Weights = weights is null ? ValueArray<double>.Empty : weights.ToValueArray();
    }

    /// <summary>Constructs a sampler whose entry count and relative probabilities come from <paramref name="weights"/>.</summary>
    public WeightedIndexSampler(IReadOnlyList<double> weights)
        : this(GetWeightCount(weights), weights)
    {
    }

    private static void CompileWeights(ValueArray<double> weights, out double totalWeight, out double[] cumulativeWeights, out int[] selectableIndices)
    {
        totalWeight = 0;
        cumulativeWeights = [];
        selectableIndices = [];

        var positiveInfinityCount = 0;
        for (var i = 0; i < weights.Count; i++)
        {
            if (double.IsPositiveInfinity(weights[i]))
                positiveInfinityCount++;
        }

        if (positiveInfinityCount > 0)
        {
            if (positiveInfinityCount < weights.Count)
                selectableIndices = GetSelectableIndices(weights, static weight => double.IsPositiveInfinity(weight), positiveInfinityCount);

            return;
        }

        var positiveWeightCount = 0;
        var maximumWeight = 0.0;
        var firstPositiveWeight = 0.0;
        var allPositiveWeightsEqual = true;
        for (var i = 0; i < weights.Count; i++)
        {
            var weight = weights[i];
            if (double.IsNaN(weight) || weight <= 0)
                continue;

            if (positiveWeightCount == 0)
                firstPositiveWeight = weight;
            else if (!weight.Equals(firstPositiveWeight))
                allPositiveWeightsEqual = false;

            positiveWeightCount++;
            maximumWeight = Math.Max(maximumWeight, weight);
        }

        if (positiveWeightCount == 0)
            return;

        if (allPositiveWeightsEqual)
        {
            if (positiveWeightCount < weights.Count)
                selectableIndices = GetSelectableIndices(weights, static weight => weight > 0, positiveWeightCount);

            return;
        }

        cumulativeWeights = new double[weights.Count];
        for (var i = 0; i < weights.Count; i++)
        {
            var weight = weights[i];
            if (weight > 0)
                totalWeight += weight / maximumWeight;

            cumulativeWeights[i] = totalWeight;
        }
    }

    private static int GetWeightCount(IReadOnlyList<double> weights)
    {
        if (weights.Count == 0)
            throw new ArgumentException("Weights must not be empty. Use the count constructor for uniform sampling.", nameof(weights));

        return weights.Count;
    }

    /// <summary>Samples an entry index in <c>[0, Count)</c>.</summary>
    public int Sample(IRandomNumberGenerator random)
    {
        if (Count == 1)
            return 0;

        if (cumulativeWeights.Length > 0)
            return SampleWeightedIndex(random.NextDouble());

        if (selectableIndices.Length == 1)
            return selectableIndices[0];

        if (selectableIndices.Length > 1)
            return selectableIndices[random.NextInt(selectableIndices.Length)];

        return random.NextInt(Count);
    }

    private int SampleWeightedIndex(double sample)
    {
        var scaledSample = sample * totalWeight;
        for (var i = 0; i < cumulativeWeights.Length; i++)
        {
            if (scaledSample < cumulativeWeights[i])
                return i;
        }

        for (var i = cumulativeWeights.Length - 1; i >= 0; i--)
        {
            if (i == 0 || cumulativeWeights[i] > cumulativeWeights[i - 1])
                return i;
        }

        throw new InvalidOperationException("Weighted sampling has no selectable entry.");
    }

    private static int[] GetSelectableIndices(IReadOnlyList<double> weights, Func<double, bool> isSelectable, int count)
    {
        var indices = new int[count];
        var resultIndex = 0;
        for (var i = 0; i < weights.Count; i++)
        {
            if (isSelectable(weights[i]))
                indices[resultIndex++] = i;
        }

        return indices;
    }

    /// <summary>
    /// Compares the fixed entry count and configured weights. The compiled sampling representation is derived from
    /// them and never participates, so a type holding a <see cref="WeightedIndexSampler"/> compares by configuration.
    /// This replaces the synthesized record equality, which would compare the compiled arrays by reference.
    /// </summary>
    public bool Equals(WeightedIndexSampler? other) =>
        other is not null && Count == other.Count && Weights.Equals(other.Weights);

    public override int GetHashCode() => HashCode.Combine(Count, Weights);

    public override string ToString() => Weights.IsEmpty ? $"uniform over {Count}" : Weights.ToString();
}
