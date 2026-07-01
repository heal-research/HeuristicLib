using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

namespace HEAL.HeuristicLib.Tests.Genotypes.SymbolicExpressions;

public sealed class SymbolicExpressionPrimitiveOperationTests
{
    [Fact]
    public void Evaluate_VariableReadsNamedDataFrameColumn()
    {
        var expression = ExpressionDraft.Variable("x0").Compile();
        var data = DataFrame.FromMatrix(
          ["x0"],
          new double[,]
          {
              { 1.0 },
              { 2.0 },
              { 3.0 }
          });

        expression.Evaluate(data).ShouldBe([1.0, 2.0, 3.0]);
    }

    [Fact]
    public void Evaluate_NumberProducesConstantSeries()
    {
        var expression = ExpressionDraft.Fixed(2.5).Compile();
        var data = DataFrame.FromOwnedColumns([
          KeyValuePair.Create("x0", new[] { 1.0, 2.0, 3.0 })
        ]);

        expression.Evaluate(data).ShouldBe([2.5, 2.5, 2.5]);
    }

    [Fact]
    public void Evaluate_AddProducesElementwiseSum()
    {
        var expression = ExpressionDraft
          .Add(ExpressionDraft.Variable("x0"), ExpressionDraft.Variable("x1"))
          .Compile();
        var data = CreateTwoColumnData();

        expression.Evaluate(data).ShouldBe([5.0, 7.0, 9.0]);
    }

    [Fact]
    public void Evaluate_SubtractProducesElementwiseDifference()
    {
        var expression = ExpressionDraft
          .Subtract(ExpressionDraft.Variable("x0"), ExpressionDraft.Variable("x1"))
          .Compile();
        var data = CreateTwoColumnData();

        expression.Evaluate(data).ShouldBe([-3.0, -3.0, -3.0]);
    }

    [Fact]
    public void Evaluate_MultiplyProducesElementwiseProduct()
    {
        var expression = ExpressionDraft
          .Multiply(ExpressionDraft.Variable("x0"), ExpressionDraft.Variable("x1"))
          .Compile();
        var data = CreateTwoColumnData();

        expression.Evaluate(data).ShouldBe([4.0, 10.0, 18.0]);
    }

    [Fact]
    public void Evaluate_DivideProducesElementwiseQuotient()
    {
        var expression = ExpressionDraft
          .Divide(ExpressionDraft.Variable("x1"), ExpressionDraft.Variable("x0"))
          .Compile();
        var data = CreateTwoColumnData();

        expression.Evaluate(data).ShouldBe([4.0, 2.5, 2.0]);
    }

    [Fact]
    public void Evaluate_LogProducesElementwiseNaturalLogarithm()
    {
        var expression = ExpressionDraft.Log(ExpressionDraft.Variable("x0")).Compile();
        var data = DataFrame.FromMatrix(
          ["x0"],
          new double[,]
          {
              { 1.0 },
              { Math.E },
              { Math.E * Math.E }
          });

        expression.Evaluate(data).ShouldBe([0.0, 1.0, 2.0], tolerance: 1e-12);
    }

    [Fact]
    public void Evaluate_SqrtProducesElementwiseSquareRoot()
    {
        var expression = ExpressionDraft.Sqrt(ExpressionDraft.Variable("x0")).Compile();
        var data = DataFrame.FromMatrix(
          ["x0"],
          new double[,]
          {
              { 1.0 },
              { 4.0 },
              { 9.0 }
          });

        expression.Evaluate(data).ShouldBe([1.0, 2.0, 3.0], tolerance: 1e-12);
    }

    [Fact]
    public void Evaluate_NestedAddCanRepresentLeftAssociativeNaryAddition()
    {
        var expression = ExpressionDraft
          .Add(
            ExpressionDraft.Add(ExpressionDraft.Variable("x0"), ExpressionDraft.Variable("x1")),
            ExpressionDraft.Variable("x2"))
          .Compile();

        expression.Evaluate(["x0", "x1", "x2"], [10.0, 2.0, 4.0]).ShouldBe(16.0);
        expression.ToInfixString().ShouldBe("((x0 + x1) + x2)");
    }

    [Fact]
    public void Evaluate_NestedSubtractCanRepresentLeftAssociativeNarySubtraction()
    {
        var expression = ExpressionDraft
          .Subtract(
            ExpressionDraft.Subtract(ExpressionDraft.Variable("x0"), ExpressionDraft.Variable("x1")),
            ExpressionDraft.Variable("x2"))
          .Compile();

        expression.Evaluate(["x0", "x1", "x2"], [10.0, 2.0, 4.0]).ShouldBe(4.0);
        expression.ToInfixString().ShouldBe("((x0 - x1) - x2)");
    }

    [Fact]
    public void Evaluate_NestedMultiplyCanRepresentLeftAssociativeNaryMultiplication()
    {
        var expression = ExpressionDraft
          .Multiply(
            ExpressionDraft.Multiply(ExpressionDraft.Variable("x0"), ExpressionDraft.Variable("x1")),
            ExpressionDraft.Variable("x2"))
          .Compile();

        expression.Evaluate(["x0", "x1", "x2"], [10.0, 2.0, 4.0]).ShouldBe(80.0);
        expression.ToInfixString().ShouldBe("((x0 * x1) * x2)");
    }

    [Fact]
    public void Evaluate_NestedDivideCanRepresentLeftAssociativeNaryDivision()
    {
        var expression = ExpressionDraft
          .Divide(
            ExpressionDraft.Divide(ExpressionDraft.Variable("x0"), ExpressionDraft.Variable("x1")),
            ExpressionDraft.Variable("x2"))
          .Compile();

        expression.Evaluate(["x0", "x1", "x2"], [64.0, 4.0, 2.0]).ShouldBe(8.0);
        expression.ToInfixString().ShouldBe("((x0 / x1) / x2)");
    }

    [Fact]
    public void Evaluate_InvalidNumericResultsUseRegularDoubleBehavior()
    {
        var divideByZero = ExpressionDraft
          .Divide(ExpressionDraft.Variable("x0"), ExpressionDraft.Variable("x1"))
          .Compile();
        var logOfNegative = ExpressionDraft.Log(ExpressionDraft.Variable("x0")).Compile();
        var sqrtOfNegative = ExpressionDraft.Sqrt(ExpressionDraft.Variable("x0")).Compile();

        divideByZero.Evaluate(["x0", "x1"], [1.0, 0.0]).ShouldBe(double.PositiveInfinity);
        double.IsNaN(logOfNegative.Evaluate(["x0"], [-1.0])).ShouldBeTrue();
        double.IsNaN(sqrtOfNegative.Evaluate(["x0"], [-1.0])).ShouldBeTrue();
    }

    [Fact]
    public void ToInfixString_FormatsPrimitiveOperations()
    {
        ExpressionDraft.Variable("x0").Compile().ToInfixString().ShouldBe("x0");
        ExpressionDraft.Fixed(2.5).Compile().ToInfixString().ShouldBe("2.5");
        ExpressionDraft.Add(ExpressionDraft.Variable("x0"), ExpressionDraft.Variable("x1"))
          .Compile().ToInfixString().ShouldBe("(x0 + x1)");
        ExpressionDraft.Subtract(ExpressionDraft.Variable("x0"), ExpressionDraft.Variable("x1"))
          .Compile().ToInfixString().ShouldBe("(x0 - x1)");
        ExpressionDraft.Multiply(ExpressionDraft.Variable("x0"), ExpressionDraft.Variable("x1"))
          .Compile().ToInfixString().ShouldBe("(x0 * x1)");
        ExpressionDraft.Divide(ExpressionDraft.Variable("x0"), ExpressionDraft.Variable("x1"))
          .Compile().ToInfixString().ShouldBe("(x0 / x1)");
        ExpressionDraft.Log(ExpressionDraft.Variable("x0")).Compile().ToInfixString().ShouldBe("log(x0)");
        ExpressionDraft.Sqrt(ExpressionDraft.Variable("x0")).Compile().ToInfixString().ShouldBe("sqrt(x0)");
    }

    private static DataFrame CreateTwoColumnData() =>
      DataFrame.FromMatrix(
        ["x0", "x1"],
        new double[,]
        {
            { 1.0, 4.0 },
            { 2.0, 5.0 },
            { 3.0, 6.0 }
        });
}
