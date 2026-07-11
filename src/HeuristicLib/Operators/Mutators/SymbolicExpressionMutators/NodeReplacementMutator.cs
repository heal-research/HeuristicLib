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
        var nodeIndex = random.NextInt(parent.NodeCount);
        var node = parent.GetNode(nodeIndex);
        var replacementSymbol = searchSpace.SelectSymbol(node.Arity, random);
        var replacement = replacementSymbol.CreateNode(random);
        return parent.WithNode(new ExpressionLocation(nodeIndex), replacement);
    }
}
