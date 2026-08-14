using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;

namespace HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionMutators;

/// <summary>Replaces one operation occurrence with a terminal sampled from the search space.</summary>
public sealed record ShrinkSubtreeMutator
    : SingleCandidateMutator<ExpressionTree, ExpressionTreeSearchSpace>
{
    public override ExpressionTree MutateCandidate(
        ExpressionTree parent,
        IRandomNumberGenerator random,
        ExpressionTreeSearchSpace searchSpace)
    {
        return ShrinkSubtreeMutation.Mutate(parent, random, searchSpace);
    }
}

public static class ShrinkSubtreeMutation
{
    public static ExpressionTree Mutate(
        ExpressionTree parent,
        IRandomNumberGenerator random,
        ExpressionTreeSearchSpace searchSpace)
    {
        var candidates = parent.RootPoint.TraversePreOrder()
            .Where(point => point.Node is OperationExpressionNode)
            .ToArray();
        if (candidates.Length == 0)
            return parent;

        var point = candidates[random.NextInt(candidates.Length)];
        var replacement = searchSpace.SelectSymbol(0, random).CreateNode(random);

        return parent.Replace(point, replacement);
    }
}
