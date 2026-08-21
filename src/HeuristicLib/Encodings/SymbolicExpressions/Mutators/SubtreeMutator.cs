using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressions;

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
