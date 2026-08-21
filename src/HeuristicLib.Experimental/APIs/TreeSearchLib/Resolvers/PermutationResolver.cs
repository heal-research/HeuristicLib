using HEAL.HeuristicLib.Encodings.Permutations;

namespace HEAL.HeuristicLib.APIs.TreeSearchLib;

public readonly struct PermutationResolver : IDecisionResolver<Permutation, int>
{
    public Permutation Resolve(IEnumerable<int> choices) => new(choices);
}
