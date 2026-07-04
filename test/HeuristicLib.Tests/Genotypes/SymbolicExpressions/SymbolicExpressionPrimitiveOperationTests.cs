using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.Genotypes.SymbolicExpressions;

public sealed class SymbolicExpressionPrimitiveOperationTests
{
    [Fact]
    public void Evaluate_VariableReadsNamedDataFrameColumn()
    {
        var expression = Variable("x0").Compile();
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
        var expression = Fixed(2.5).Compile();
        var data = DataFrame.FromOwnedColumns([
          KeyValuePair.Create("x0", new[] { 1.0, 2.0, 3.0 })
        ]);

        expression.Evaluate(data).ShouldBe([2.5, 2.5, 2.5]);
    }

    [Fact]
    public void Evaluate_AddProducesElementwiseSum()
    {
        var expression = (Variable("x0") + Variable("x1")).Compile();
        var data = CreateTwoColumnData();

        expression.Evaluate(data).ShouldBe([5.0, 7.0, 9.0]);
    }

    [Fact]
    public void Evaluate_SubtractProducesElementwiseDifference()
    {
        var expression = (Variable("x0") - Variable("x1")).Compile();
        var data = CreateTwoColumnData();

        expression.Evaluate(data).ShouldBe([-3.0, -3.0, -3.0]);
    }

    [Fact]
    public void Evaluate_MultiplyProducesElementwiseProduct()
    {
        var expression = (Variable("x0") * Variable("x1")).Compile();
        var data = CreateTwoColumnData();

        expression.Evaluate(data).ShouldBe([4.0, 10.0, 18.0]);
    }

    [Fact]
    public void Evaluate_DivideProducesElementwiseQuotient()
    {
        var expression = (Variable("x1") / Variable("x0")).Compile();
        var data = CreateTwoColumnData();

        expression.Evaluate(data).ShouldBe([4.0, 2.5, 2.0]);
    }

    [Fact]
    public void Evaluate_LogProducesElementwiseNaturalLogarithm()
    {
        var expression = Log(Variable("x0")).Compile();
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
        var expression = Sqrt(Variable("x0")).Compile();
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
        var expression = ((Variable("x0") + Variable("x1")) + Variable("x2")).Compile();

        expression.Evaluate(["x0", "x1", "x2"], [10.0, 2.0, 4.0]).ShouldBe(16.0);
        expression.ToInfixString().ShouldBe("((x0 + x1) + x2)");
    }

    [Fact]
    public void Evaluate_NestedSubtractCanRepresentLeftAssociativeNarySubtraction()
    {
        var expression = ((Variable("x0") - Variable("x1")) - Variable("x2")).Compile();

        expression.Evaluate(["x0", "x1", "x2"], [10.0, 2.0, 4.0]).ShouldBe(4.0);
        expression.ToInfixString().ShouldBe("((x0 - x1) - x2)");
    }

    [Fact]
    public void Evaluate_NestedMultiplyCanRepresentLeftAssociativeNaryMultiplication()
    {
        var expression = ((Variable("x0") * Variable("x1")) * Variable("x2")).Compile();

        expression.Evaluate(["x0", "x1", "x2"], [10.0, 2.0, 4.0]).ShouldBe(80.0);
        expression.ToInfixString().ShouldBe("((x0 * x1) * x2)");
    }

    [Fact]
    public void Evaluate_NestedDivideCanRepresentLeftAssociativeNaryDivision()
    {
        var expression = ((Variable("x0") / Variable("x1")) / Variable("x2")).Compile();

        expression.Evaluate(["x0", "x1", "x2"], [64.0, 4.0, 2.0]).ShouldBe(8.0);
        expression.ToInfixString().ShouldBe("((x0 / x1) / x2)");
    }

    [Fact]
    public void Evaluate_InvalidNumericResultsUseRegularDoubleBehavior()
    {
        var divideByZero = (Variable("x0") / Variable("x1")).Compile();
        var logOfNegative = Log(Variable("x0")).Compile();
        var sqrtOfNegative = Sqrt(Variable("x0")).Compile();

        divideByZero.Evaluate(["x0", "x1"], [1.0, 0.0]).ShouldBe(double.PositiveInfinity);
        double.IsNaN(logOfNegative.Evaluate(["x0"], [-1.0])).ShouldBeTrue();
        double.IsNaN(sqrtOfNegative.Evaluate(["x0"], [-1.0])).ShouldBeTrue();
    }

    [Fact]
    public void ToInfixString_FormatsPrimitiveOperations()
    {
        Variable("x0").Compile().ToInfixString().ShouldBe("x0");
        Fixed(2.5).Compile().ToInfixString().ShouldBe("2.5");
        Add(Variable("x0"), Variable("x1")).Compile().ToInfixString().ShouldBe("(x0 + x1)");
        Subtract(Variable("x0"), Variable("x1")).Compile().ToInfixString().ShouldBe("(x0 - x1)");
        Multiply(Variable("x0"), Variable("x1")).Compile().ToInfixString().ShouldBe("(x0 * x1)");
        Divide(Variable("x0"), Variable("x1")).Compile().ToInfixString().ShouldBe("(x0 / x1)");
        Log(Variable("x0")).Compile().ToInfixString().ShouldBe("log(x0)");
        Sqrt(Variable("x0")).Compile().ToInfixString().ShouldBe("sqrt(x0)");
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
