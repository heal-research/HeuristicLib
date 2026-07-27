using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Optimization;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.Genotypes.SymbolicExpressions;

public sealed class ExpressionMetricTests
{
    [Fact]
    public void Length_CountsExpressionNodes()
    {
        var expression = CreateExpression();

        ExpressionMetrics.Length.Evaluate(expression).ShouldBe(5.0);
        ExpressionMetrics.Length.Direction.ShouldBe(ObjectiveDirection.Minimize);
    }

    [Fact]
    public void VariableCount_CountsOccurrences()
    {
        var expression = CreateExpression();

        ExpressionMetrics.VariableCount.Evaluate(expression).ShouldBe(2.0);
        ExpressionMetrics.VariableCount.Direction.ShouldBe(ObjectiveDirection.Minimize);
    }

    [Fact]
    public void Complexity_UsesSymbolicRegressionComplexityRules()
    {
        var expression = CreateExpression();

        ExpressionMetrics.Complexity.Evaluate(expression).ShouldBe(8.0);
        ExpressionMetrics.Complexity.Direction.ShouldBe(ObjectiveDirection.Minimize);
    }

    private static ExpressionTree CreateExpression()
    {
        return (Variable("x0") + FixedConstant(2.0) * Variable("x1")).Build();
    }
}
