using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public static class ExpressionMetrics
{
    public static IExpressionMetric Length { get; } = new ExpressionLengthMetric();
    public static IExpressionMetric VariableCount { get; } = new ExpressionVariableCountMetric();
    public static IExpressionMetric Complexity { get; } = new ExpressionComplexityMetric();
}

public sealed class ExpressionLengthMetric : IExpressionMetric
{
    public ObjectiveDirection Direction => ObjectiveDirection.Minimize;

    public double Evaluate(ExpressionTree expression)
    {
        return expression.Length;
    }
}

public sealed class ExpressionVariableCountMetric : IExpressionMetric
{
    public ObjectiveDirection Direction => ObjectiveDirection.Minimize;

    public double Evaluate(ExpressionTree expression)
    {
        var count = 0;
        foreach (var node in expression.TraversePreOrder())
        {
            if (node is VariableExpressionNode)
                count++;
        }

        return count;
    }
}

public sealed class ExpressionComplexityMetric : IExpressionMetric
{
    public ObjectiveDirection Direction => ObjectiveDirection.Minimize;

    public double Evaluate(ExpressionTree expression)
    {
        return Evaluate(expression.Root);
    }

    private static double Evaluate(ExpressionNode node)
    {
        return node switch
        {
            VariableExpressionNode => 2.0,
            TerminalExpressionNode => 1.0,
            UnaryExpressionNode { Symbol: NegationSymbol } unary =>
                Evaluate(unary.Operand),
            UnaryExpressionNode { Symbol: ExponentialSymbol or LogarithmSymbol } unary =>
                Math.Pow(2.0, Evaluate(unary.Operand)),
            UnaryExpressionNode { Symbol: SquareRootSymbol } unary =>
                Math.Pow(Evaluate(unary.Operand), 3.0),
            BinaryExpressionNode { Symbol: AdditionSymbol or SubtractionSymbol } binary =>
                Evaluate(binary.Left) + Evaluate(binary.Right),
            BinaryExpressionNode { Symbol: MultiplicationSymbol or DivisionSymbol } binary =>
                (1.0 + Evaluate(binary.Left)) * (1.0 + Evaluate(binary.Right)),
            OperationExpressionNode operation =>
                EvaluateGenericOperation(operation),
            _ => throw new NotSupportedException(
                $"Expression complexity is not defined for node type '{node.GetType().Name}'.")
        };
    }

    private static double EvaluateGenericOperation(OperationExpressionNode operation)
    {
        var complexity = 1.0;
        for (var i = 0; i < operation.Arity; i++)
            complexity += Evaluate(operation.GetChild(i));

        return complexity;
    }
}
