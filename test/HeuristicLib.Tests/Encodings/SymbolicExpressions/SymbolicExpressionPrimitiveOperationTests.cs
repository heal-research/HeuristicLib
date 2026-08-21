using static HEAL.HeuristicLib.Encodings.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.Genotypes.SymbolicExpressions;

public sealed class SymbolicExpressionPrimitiveOperationTests
{
    [Fact]
    public void Evaluate_VariableReadsNamedDataFrameColumn()
    {
        var expression = Variable("x0").Build();
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
        var expression = FixedConstant(2.5).Build();
        var data = new DataFrame([
            Series<double>.FromOwnedArray("x0", [1.0, 2.0, 3.0])
        ]);

        expression.Evaluate(data).ShouldBe([2.5, 2.5, 2.5]);
    }

    [Fact]
    public void Evaluate_AddProducesElementwiseSum()
    {
        var expression = (Variable("x0") + Variable("x1")).Build();
        var data = CreateTwoColumnData();

        expression.Evaluate(data).ShouldBe([5.0, 7.0, 9.0]);
    }

    [Fact]
    public void Evaluate_SubtractProducesElementwiseDifference()
    {
        var expression = (Variable("x0") - Variable("x1")).Build();
        var data = CreateTwoColumnData();

        expression.Evaluate(data).ShouldBe([-3.0, -3.0, -3.0]);
    }

    [Fact]
    public void Evaluate_MultiplyProducesElementwiseProduct()
    {
        var expression = (Variable("x0") * Variable("x1")).Build();
        var data = CreateTwoColumnData();

        expression.Evaluate(data).ShouldBe([4.0, 10.0, 18.0]);
    }

    [Fact]
    public void Evaluate_DivideProducesElementwiseQuotient()
    {
        var expression = (Variable("x1") / Variable("x0")).Build();
        var data = CreateTwoColumnData();

        expression.Evaluate(data).ShouldBe([4.0, 2.5, 2.0]);
    }

    [Fact]
    public void Evaluate_LogProducesElementwiseNaturalLogarithm()
    {
        var expression = Log(Variable("x0")).Build();
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
        var expression = Sqrt(Variable("x0")).Build();
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
    public void Evaluate_TrigonometricOperationsProduceElementwiseResults()
    {
        var data = new DataFrame([
            Series<double>.FromOwnedArray("x", [0.0, Math.PI / 4.0, Math.PI / 2.0])
        ]);

        Sin(Variable("x")).Build().Evaluate(data).ShouldBe([0.0, Math.Sqrt(0.5), 1.0], tolerance: 1e-12);
        Cos(Variable("x")).Build().Evaluate(data).ShouldBe([1.0, Math.Sqrt(0.5), 0.0], tolerance: 1e-12);
        Tan(Variable("x")).Build().Evaluate(data)[1].ShouldBe(1.0, tolerance: 1e-12);
        Tanh(Variable("x")).Build().Evaluate(data).ShouldBe(
            [0.0, Math.Tanh(Math.PI / 4.0), Math.Tanh(Math.PI / 2.0)],
            tolerance: 1e-12);
    }

    [Fact]
    public void Evaluate_UnaryAlgebraicOperationsProduceElementwiseResults()
    {
        var data = new DataFrame([
            Series<double>.FromOwnedArray("x", [-8.0, -2.0, 3.0])
        ]);

        Abs(Variable("x")).Build().Evaluate(data).ShouldBe([8.0, 2.0, 3.0]);
        Square(Variable("x")).Build().Evaluate(data).ShouldBe([64.0, 4.0, 9.0]);
        Cube(Variable("x")).Build().Evaluate(data).ShouldBe([-512.0, -8.0, 27.0]);
        CubeRoot(Variable("x")).Build().Evaluate(data).ShouldBe([-2.0, -Math.Cbrt(2.0), Math.Cbrt(3.0)], tolerance: 1e-12);
    }

    [Fact]
    public void Evaluate_PowerAndRootUseUnroundedOperands()
    {
        var data = new DataFrame([
            Series<double>.FromOwnedArray("value", [9.0, 32.0]),
            Series<double>.FromOwnedArray("argument", [0.5, 2.5])
        ]);

        Power(Variable("value"), Variable("argument")).Build().Evaluate(data)
            .ShouldBe([3.0, Math.Pow(32.0, 2.5)], tolerance: 1e-12);
        Root(Variable("value"), Variable("argument")).Build().Evaluate(data)
            .ShouldBe([81.0, 4.0], tolerance: 1e-12);
    }

    [Fact]
    public void Evaluate_PowerAndRootHandleScalarAndVectorOperands()
    {
        var data = new DataFrame([
            Series<double>.FromOwnedArray("value", [4.0, 9.0]),
            Series<double>.FromOwnedArray("argument", [2.0, 0.5])
        ]);

        Power(Variable("value"), FixedConstant(0.5)).Build().Evaluate(data)
            .ShouldBe([2.0, 3.0], tolerance: 1e-12);
        Power(FixedConstant(9.0), Variable("argument")).Build().Evaluate(data)
            .ShouldBe([81.0, 3.0], tolerance: 1e-12);
        Root(Variable("value"), FixedConstant(2.0)).Build().Evaluate(data)
            .ShouldBe([2.0, 3.0], tolerance: 1e-12);
        Root(FixedConstant(16.0), Variable("argument")).Build().Evaluate(data)
            .ShouldBe([4.0, 256.0], tolerance: 1e-12);
    }

    [Fact]
    public void Evaluate_AnalyticQuotientProducesElementwiseResults()
    {
        var data = new DataFrame([
            Series<double>.FromOwnedArray("numerator", [2.0, 6.0, -10.0]),
            Series<double>.FromOwnedArray("denominator", [0.0, Math.Sqrt(3.0), 0.0])
        ]);

        AnalyticQuotient(Variable("numerator"), Variable("denominator")).Build().Evaluate(data)
            .ShouldBe([2.0, 3.0, -10.0], tolerance: 1e-12);
        AnalyticQuotient(Variable("numerator"), FixedConstant(0.0)).Build().Evaluate(data)
            .ShouldBe([2.0, 6.0, -10.0], tolerance: 1e-12);
        AnalyticQuotient(FixedConstant(6.0), Variable("denominator")).Build().Evaluate(data)
            .ShouldBe([6.0, 3.0, 6.0], tolerance: 1e-12);
    }

    [Fact]
    public void Evaluate_NestedAddCanRepresentLeftAssociativeNaryAddition()
    {
        var expression = ((Variable("x0") + Variable("x1")) + Variable("x2")).Build();

        expression.EvaluateSingleRow(("x0", 10.0), ("x1", 2.0), ("x2", 4.0)).ShouldBe(16.0);
        expression.ToInfixString().ShouldBe("((x0 + x1) + x2)");
    }

    [Fact]
    public void Evaluate_NestedSubtractCanRepresentLeftAssociativeNarySubtraction()
    {
        var expression = ((Variable("x0") - Variable("x1")) - Variable("x2")).Build();

        expression.EvaluateSingleRow(("x0", 10.0), ("x1", 2.0), ("x2", 4.0)).ShouldBe(4.0);
        expression.ToInfixString().ShouldBe("((x0 - x1) - x2)");
    }

    [Fact]
    public void Evaluate_NestedMultiplyCanRepresentLeftAssociativeNaryMultiplication()
    {
        var expression = ((Variable("x0") * Variable("x1")) * Variable("x2")).Build();

        expression.EvaluateSingleRow(("x0", 10.0), ("x1", 2.0), ("x2", 4.0)).ShouldBe(80.0);
        expression.ToInfixString().ShouldBe("((x0 * x1) * x2)");
    }

    [Fact]
    public void Evaluate_NestedDivideCanRepresentLeftAssociativeNaryDivision()
    {
        var expression = ((Variable("x0") / Variable("x1")) / Variable("x2")).Build();

        expression.EvaluateSingleRow(("x0", 64.0), ("x1", 4.0), ("x2", 2.0)).ShouldBe(8.0);
        expression.ToInfixString().ShouldBe("((x0 / x1) / x2)");
    }

    [Fact]
    public void Evaluate_InvalidNumericResultsUseRegularDoubleBehavior()
    {
        var divideByZero = (Variable("x0") / Variable("x1")).Build();
        var logOfNegative = Log(Variable("x0")).Build();
        var sqrtOfNegative = Sqrt(Variable("x0")).Build();

        divideByZero.EvaluateSingleRow(("x0", 1.0), ("x1", 0.0)).ShouldBe(double.PositiveInfinity);
        double.IsNaN(logOfNegative.EvaluateSingleRow(("x0", -1.0))).ShouldBeTrue();
        double.IsNaN(sqrtOfNegative.EvaluateSingleRow(("x0", -1.0))).ShouldBeTrue();
    }

    [Fact]
    public void ToInfixString_FormatsPrimitiveOperations()
    {
        Variable("x0").Build().ToInfixString().ShouldBe("x0");
        FixedConstant(2.5).Build().ToInfixString().ShouldBe("2.5");
        Add(Variable("x0"), Variable("x1")).Build().ToInfixString().ShouldBe("(x0 + x1)");
        Subtract(Variable("x0"), Variable("x1")).Build().ToInfixString().ShouldBe("(x0 - x1)");
        Multiply(Variable("x0"), Variable("x1")).Build().ToInfixString().ShouldBe("(x0 * x1)");
        Divide(Variable("x0"), Variable("x1")).Build().ToInfixString().ShouldBe("(x0 / x1)");
        Log(Variable("x0")).Build().ToInfixString().ShouldBe("log(x0)");
        Sqrt(Variable("x0")).Build().ToInfixString().ShouldBe("sqrt(x0)");
        Sin(Variable("x0")).Build().ToInfixString().ShouldBe("sin(x0)");
        CubeRoot(Variable("x0")).Build().ToInfixString().ShouldBe("cbrt(x0)");
        Power(Variable("x0"), FixedConstant(2.5)).Build().ToInfixString().ShouldBe("pow(x0, 2.5)");
        Root(Variable("x0"), FixedConstant(3.0)).Build().ToInfixString().ShouldBe("root(x0, 3)");
        AnalyticQuotient(Variable("x0"), Variable("x1")).Build().ToInfixString().ShouldBe("aq(x0, x1)");
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
