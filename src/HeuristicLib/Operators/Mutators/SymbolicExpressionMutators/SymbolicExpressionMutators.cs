using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;

namespace HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionMutators;

public static class SymbolicExpressionMutators
{
    /// <summary>
    /// Gets the standard symbolic-regression mutation set. Selection strategy and weights are defined by its consumer.
    /// </summary>
    public static ImmutableArray<SingleCandidateMutator<ExpressionTree, ExpressionTreeSearchSpace>> Default { get; } =
    [
        new NodeReplacementMutator(),
        new LocalPerturbationMutator(LocalPerturbationTargets.All),
        new LocalPerturbationMutator(),
        new ShrinkSubtreeMutator(),
        new SubtreeMutator()
    ];
}
