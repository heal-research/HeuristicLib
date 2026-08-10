using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Mutators.IntegerVectorMutators;

public record UniformOnePositionMutator
    : SingleCandidateMutator<IntegerVector, IntegerVectorSearchSpace>
{
    public override IntegerVector MutateCandidate(IntegerVector parent, IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace) =>
        Mutate(parent, random, searchSpace);

    public static IntegerVector Mutate(IntegerVector candidate, IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace)
    {
        if (candidate.Count != searchSpace.Length)
            throw new ArgumentException("Candidate length must match the search space length.", nameof(candidate));

        return Mutate(candidate, random, searchSpace.Minimum, searchSpace.Maximum);
    }

    public static IntegerVector Mutate(IntegerVector candidate, IRandomNumberGenerator random, IntegerVector minimum, IntegerVector maximum)
    {
        if (!IntegerVector.AreBroadcastableTo(candidate.Count, minimum, maximum))
            throw new ArgumentException("Minimum and maximum must each have length 1 or match the candidate length.");

        if (candidate.Count == 0)
            return candidate;

        var index = random.NextInt(0, candidate.Count);
        var res = candidate.ToArray();
        res[index] = random.NextIntegerVectorUniformAt(minimum, maximum, index);
        return IntegerVector.FromOwnedArray(res);
    }
}
