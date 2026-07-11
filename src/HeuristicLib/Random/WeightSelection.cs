namespace HEAL.HeuristicLib.Random;

public static class WeightSelection
{
    public static ImmutableArray<double> Normalize(IReadOnlyList<double>? weights, int expectedCount)
    {
        if (weights is null)
            return ImmutableArray<double>.Empty;
        if (weights.Count != expectedCount)
            throw new ArgumentException("Weights must have the expected number of entries.", nameof(weights));

        var total = 0.0;
        var firstWeight = weights[0];
        var allEqual = true;
        foreach (var weight in weights)
        {
            if (!double.IsFinite(weight) || weight < 0.0)
                throw new ArgumentOutOfRangeException(nameof(weights), "Weights must be finite and non-negative.");

            total += weight;
            allEqual &= weight == firstWeight;
        }

        if (total <= 0.0)
            throw new ArgumentException("At least one weight must be positive.", nameof(weights));
        if (allEqual)
            return ImmutableArray<double>.Empty;

        return weights.Select(weight => weight / total).ToImmutableArray();
    }

    public static int SelectIndex(IRandomNumberGenerator random, int count, ImmutableArray<double> normalizedWeights)
    {
        if (normalizedWeights.IsEmpty)
            return random.NextInt(count);

        var value = random.NextDouble();
        for (var i = 0; i < normalizedWeights.Length; i++)
        {
            value -= normalizedWeights[i];
            if (value < 0.0)
                return i;
        }

        return normalizedWeights.Length - 1;
    }
}
