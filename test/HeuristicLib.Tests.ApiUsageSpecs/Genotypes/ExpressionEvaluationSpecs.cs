using HEAL.HeuristicLib.Data;
using HEAL.HeuristicLib.Encodings.SymbolicExpressions;
using static HEAL.HeuristicLib.Encodings.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Genotypes;

public class ExpressionEvaluationSpecs
{
    [Fact]
    public void EvaluatingATreeDirectlyIsTheOneShotShape()
    {
        var expression = (Variable("x") * FixedConstant(2.0) + FixedConstant(1.0)).Build();

        expression.Evaluate(CreateData()).ShouldBe([3.0, 5.0, 7.0]);
    }

    // Evaluating a tree compiles it first, so the same tree evaluated in a loop is compiled every time. Compiling once
    // and evaluating the compiled expression is the same call on a different receiver, which is the shape to reach for
    // whenever one expression is evaluated against more than one batch of rows.
    [Fact]
    public void CompilingOnceIsTheShapeForRepeatedEvaluation()
    {
        var expression = (Variable("x") * FixedConstant(2.0) + FixedConstant(1.0)).Build();
        var compiled = expression.Compile();

        foreach (var batch in new[] { CreateData(), CreateData() })
        {
            compiled.Evaluate(batch).ShouldBe([3.0, 5.0, 7.0]);
        }
    }

    [Fact]
    public void BothShapesAgreeAndAcceptACallerSuppliedDestination()
    {
        var expression = (Variable("x") * FixedConstant(2.0) + FixedConstant(1.0)).Build();
        var data = CreateData();
        var compiled = expression.Compile();

        var fromTree = new double[data.RowCount];
        var fromCompiled = new double[data.RowCount];
        expression.Evaluate(data, fromTree);
        compiled.Evaluate(data, fromCompiled);

        fromCompiled.ShouldBe(fromTree);
        fromCompiled.ShouldBe(expression.Evaluate(data));
    }

    [Fact]
    public void SingleRowEvaluationIsAvailableOnBothShapes()
    {
        var expression = (Variable("x") * FixedConstant(2.0) + FixedConstant(1.0)).Build();

        expression.EvaluateSingleRow(("x", 4.0)).ShouldBe(9.0);
        expression.Compile().EvaluateSingleRow(("x", 4.0)).ShouldBe(9.0);
    }

    private static DataFrame CreateData() =>
        DataFrame.FromMatrix(["x"], new[,] { { 1.0 }, { 2.0 }, { 3.0 } });
}
