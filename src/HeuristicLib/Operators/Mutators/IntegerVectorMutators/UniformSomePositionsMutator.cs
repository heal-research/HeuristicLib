using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Mutators.IntegerVectorMutators;

/// <summary>
/// Mutates each position independently with probability <see cref="Probability"/> by
/// replacing it with a uniformly sampled feasible value from the search space bounds.
/// </summary>
public record UniformSomePositionsMutator
    : SingleCandidateMutator<IntegerVector, IntegerVectorSearchSpace>
{
    public double Probability { get; init; } = 0.05;

    public bool AtLeastOnce { get; init; } = true;

    public override IntegerVector MutateCandidate(IntegerVector parent, IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace) =>
        Mutate(parent, random, searchSpace, Probability, AtLeastOnce);

    public static IntegerVector Mutate(IntegerVector candidate, IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace, double probability, bool atLeastOnce = true)
    {
        return Mutate(candidate, random, searchSpace.Minimum, searchSpace.Maximum, probability, atLeastOnce);
    }

    public static IntegerVector Mutate(IntegerVector candidate, IRandomNumberGenerator random, IntegerVector minimum, IntegerVector maximum, double probability, bool atLeastOnce = true)
    {
        if (!Vector.AreBroadcastableTo(candidate.Count, minimum, maximum))
            throw new ArgumentException("Minimum and maximum must each have length 1 or match the candidate length.");

        if (candidate.Count == 0)
            return candidate;

        var res = candidate.ToArray();
        var mutated = false;
        var length = candidate.Count;
        for (var i = 0; i < length; i++)
        {
            if (random.NextDouble() < probability)
            {
                mutated = true;
                res[i] = random.NextIntegerVectorUniformAt(minimum, maximum, i);
            }
        }

        if (!mutated && atLeastOnce)
        {
            var idx = random.NextInt(0, candidate.Count);
            res[idx] = random.NextIntegerVectorUniformAt(minimum, maximum, idx);
        }

        return IntegerVector.FromOwnedArray(res);
    }
}
