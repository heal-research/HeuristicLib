using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Mutators.IntegerVectorMutators;

public record RoundedNormalOnePositionMutator
    : SingleCandidateMutator<IntegerVector, IntegerVectorSearchSpace>
{
    /// <summary>
    /// Standard deviations per dimension. A scalar is broadcast to every dimension.
    /// </summary>
    public RealVector Sigma { get; init; } = new(1.0);

    public override IntegerVector MutateCandidate(IntegerVector parent, IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace) =>
        Mutate(parent, random, searchSpace, Sigma);

    public static IntegerVector Mutate(IntegerVector candidate, IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace, RealVector sigma)
    {
        return Mutate(candidate, random, searchSpace.Minimum, searchSpace.Maximum, sigma);
    }

    public static IntegerVector Mutate(IntegerVector candidate, IRandomNumberGenerator random, IntegerVector minimum, IntegerVector maximum, RealVector sigma)
    {
        if (!Vector.AreBroadcastableTo(candidate.Count, minimum, maximum, sigma))
            throw new ArgumentException("Minimum, maximum, and sigma must each have length 1 or match the candidate length.");

        var length = candidate.Count;
        if (length == 0)
            return candidate;

        var idx = random.NextInt(0, length); // upper-exclusive

        var s = sigma.Count == 1 ? sigma[0] : sigma[idx];
        var result = candidate.ToArray();
        var value = random.NextNormal(candidate[idx], s);
        result[idx] = RealVector.RoundToIntegerAt(value, minimum, maximum, idx);

        return IntegerVector.FromOwnedArray(result);
    }
}
