using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressions;

public sealed record NodeReplacementMutator
    : SingleCandidateMutator<ExpressionTree, ExpressionTreeSearchSpace>
{
    public override ExpressionTree MutateCandidate(ExpressionTree parent, IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace) =>
        NodeReplacementMutation.Mutate(parent, random, searchSpace);
}

public static class NodeReplacementMutation
{
    public static ExpressionTree Mutate(ExpressionTree parent, IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace)
    {
        var point = parent.GetPoint(random.NextInt(parent.Length));
        var replacementSymbol = searchSpace.SelectSymbol(point.Node.Arity, random);
        var replacement = ReplaceSymbol(point.Node, replacementSymbol, random);
        return point.ReplaceWith(replacement);
    }

    private static ExpressionNode ReplaceSymbol(ExpressionNode node, Symbol replacementSymbol, IRandomNumberGenerator random)
    {
        return (node, replacementSymbol) switch
        {
            (TerminalExpressionNode, { Arity: 0 } symbol) => symbol.CreateNode(random),
            (UnaryExpressionNode unary, OperationSymbol { Arity: 1 } symbol) => new UnaryExpressionNode(symbol, unary.Operand),
            (BinaryExpressionNode binary, OperationSymbol { Arity: 2 } symbol) => new BinaryExpressionNode(symbol, binary.Left, binary.Right),
            (NaryExpressionNode nary, OperationSymbol symbol) when symbol.Arity == nary.Arity => new NaryExpressionNode(symbol, nary.Children),
            _ => throw new ArgumentException($"Symbol '{replacementSymbol.Name}' cannot replace node '{node.Name}'.", nameof(replacementSymbol))
        };
    }
}
