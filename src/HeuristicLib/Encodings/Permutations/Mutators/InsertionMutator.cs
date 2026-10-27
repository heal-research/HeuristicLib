using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.Permutations;

/// <summary>Moves one randomly chosen element to a randomly chosen position.</summary>
/// <remarks>Permutations with fewer than two elements are returned unchanged without random draws.</remarks>
public sealed record InsertionMutator : SingleCandidateMutator<Permutation>
{
    public override Permutation MutateCandidate(Permutation parent, IRandomNumberGenerator random) =>
        Mutate(parent, random);

    public static Permutation Mutate(Permutation parent, IRandomNumberGenerator random)
    {
        if (parent.Count < 2)
            return parent;

        var cutIndex = random.NextInt(parent.Count);
        var insertIndex = random.NextInt(parent.Count);
        if (cutIndex == insertIndex)
            return parent;

        return TranslocationMutator.Mutate(parent, cutIndex, cutIndex, insertIndex);
    }
}
