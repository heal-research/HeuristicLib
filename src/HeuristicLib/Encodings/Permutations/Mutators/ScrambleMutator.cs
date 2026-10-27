using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.Permutations;

/// <summary>Shuffles the values in a randomly chosen inclusive segment using Fisher–Yates.</summary>
/// <remarks>Permutations with fewer than two elements are returned unchanged without random draws.</remarks>
public sealed record ScrambleMutator : SingleCandidateMutator<Permutation>
{
    public override Permutation MutateCandidate(Permutation parent, IRandomNumberGenerator random) =>
        Mutate(parent, random);

    public static Permutation Mutate(Permutation parent, IRandomNumberGenerator random)
    {
        if (parent.Count < 2)
            return parent;

        var (start, end) = OrderCrossover.GetRandomBreakPoints(parent.Count, random);
        var result = parent.ToArray();
        for (var i = end; i > start; i--)
        {
            var j = random.NextInt(start, i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }

        return Permutation.FromOwnedArray(result);
    }
}
