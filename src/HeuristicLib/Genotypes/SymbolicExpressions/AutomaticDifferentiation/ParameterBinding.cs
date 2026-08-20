namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions.AutomaticDifferentiation;

internal sealed class ParameterBinding
{
    internal ParameterBinding(ExpressionPoint point)
    {
        if (point.Node is not NumericConstantExpressionNode { Symbol: EvolvableConstantSymbol })
            throw new ArgumentException("A parameter binding requires an evolvable numeric constant occurrence.", nameof(point));

        Point = point;
    }

    internal ExpressionPoint Point { get; }
    internal EvolvableConstantSymbol Symbol => (EvolvableConstantSymbol)Constant.Symbol;
    internal double InitialValue => Constant.Value;

    internal (ExpressionPoint Point, ExpressionNode Replacement) CreateReplacement(double value) =>
        (Point, new NumericConstantExpressionNode(Symbol, value));

    private NumericConstantExpressionNode Constant => (NumericConstantExpressionNode)Point.Node;
}
