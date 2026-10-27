using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.Permutations;

/// <summary>Rotates the values at three distinct, randomly chosen positions.</summary>
/// <remarks>Requires at least three elements.</remarks>
public sealed record Swap3Mutator : SingleCandidateMutator<Permutation>
{
    public override Permutation MutateCandidate(Permutation parent, IRandomNumberGenerator random) =>
        Mutate(parent, random);

    public static Permutation Mutate(Permutation parent, IRandomNumberGenerator random)
    {
        if (parent.Count < 3)
            throw new ArgumentException("Three-position swap requires at least three elements.", nameof(parent));

        // Sample without replacement so even a constant RNG terminates.
        var index1 = random.NextInt(parent.Count);
        var index2 = random.NextInt(parent.Count - 1);
        if (index2 >= index1)
            index2++;
        var index3 = random.NextInt(parent.Count - 2);
        if (index3 >= Math.Min(index1, index2))
            index3++;
        if (index3 >= Math.Max(index1, index2))
            index3++;

        var result = parent.ToArray();
        (result[index1], result[index2], result[index3]) = (result[index2], result[index3], result[index1]);
        return Permutation.FromOwnedArray(result);
    }
}
