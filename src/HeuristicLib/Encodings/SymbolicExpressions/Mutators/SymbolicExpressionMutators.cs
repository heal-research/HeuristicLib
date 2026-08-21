using HEAL.HeuristicLib.Operators.Mutators;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressions;

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
