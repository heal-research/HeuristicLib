using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Mutators.PermutationMutators;

public record InversionMutator : SingleCandidateMutator<Permutation>
{
    public override Permutation MutateCandidate(Permutation parent, IRandomNumberGenerator random) =>
        parent.InvertRandomRange(random);
}
