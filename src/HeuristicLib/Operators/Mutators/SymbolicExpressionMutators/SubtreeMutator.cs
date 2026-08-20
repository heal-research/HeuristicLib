using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Operators.Creators.SymbolicExpressionCreators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;

namespace HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionMutators;

public sealed record SubtreeMutator
    : SingleCandidateMutator<ExpressionTree, ExpressionTreeSearchSpace>
{
    public override ExpressionTree MutateCandidate(ExpressionTree parent, IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace) =>
        SubtreeMutation.Mutate(parent, random, searchSpace);
}

public static class SubtreeMutation
{
    public static ExpressionTree Mutate(ExpressionTree parent, IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace)
    {
        var point = parent.GetPoint(random.NextInt(parent.Length));
        var unchangedLength = parent.Length - point.Node.Length;
        var maximumSubtreeLength = searchSpace.MaximumLength - unchangedLength;
        var maximumSubtreeDepth = searchSpace.MaximumDepth - point.Depth;
        var replacement = GrowTreeCreation.CreateSubtree(random, searchSpace, maximumSubtreeLength, maximumSubtreeDepth);

        return parent.Replace(point, replacement);
    }
}
