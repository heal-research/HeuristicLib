using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;

namespace HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionMutators;

public sealed record NodeReplacementMutator
    : SingleSolutionMutator<ExpressionTree, ExpressionTreeSearchSpace>
{
    public override ExpressionTree Mutate(ExpressionTree parent, IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace) =>
        NodeReplacementMutation.Mutate(parent, random, searchSpace);
}

public static class NodeReplacementMutation
{
    public static ExpressionTree Mutate(ExpressionTree parent, IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace)
    {
        var point = parent.GetPoint(random.NextInt(parent.Length));
        var replacementSymbol = searchSpace.SelectSymbol(point.Node.Arity, random);
        var replacement = ExpressionNode.FromOwnedChildren(replacementSymbol, random, point.Node.Children);
        return point.ReplaceWith(replacement);
    }
}
