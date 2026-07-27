using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.DataAnalysis.Formatting;

public sealed class ExpressionFormatterTests
{
    [Fact]
    public void Infix_PreservesMacros()
    {
        var expression = Sigmoid(Variable("x")).Build();

        ExpressionFormatters.Infix.Format(expression).ShouldBe("sigmoid(x)");
        expression.ToInfixString().ShouldBe("sigmoid(x)");
    }

    [Fact]
    public void Infix_DistinguishesEvolvableAndFixedConstants()
    {
        var expression = (Constant(2) + FixedConstant(3)).Build();

        expression.ToInfixString().ShouldBe("(2 + 3)");
        expression.ToInfixString(InfixConstantNotation.MarkParameters).ShouldBe("(param(2) + 3)");
        expression.ToInfixString(InfixConstantNotation.MarkFixedConstants).ShouldBe("(2 + fixed(3))");
        expression.ToInfixString(InfixConstantNotation.MarkAll).ShouldBe("(param(2) + fixed(3))");
    }

    [Fact]
    public void Infix_RejectsInvalidConstantNotation()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new InfixExpressionFormatter((InfixConstantNotation)int.MaxValue));
        Should.Throw<ArgumentOutOfRangeException>(() =>
            ExpressionFormatters.Infix.Format(FixedConstant(1).Build(), (InfixConstantNotation)int.MaxValue));
    }

    [Fact]
    public void CSharp_FormatsMaintainedOperationsAndIdentifiers()
    {
        var expression = Power(Variable("class"), FixedConstant(2.5))
            + AnalyticQuotient(Variable("sensor value"), Variable("scale"));

        expression.Build().ToCSharpString().ShouldBe(
            "(Math.Pow(_class, 2.5) + (sensor_value / Math.Sqrt(1.0 + (scale * scale))))");
    }

    [Fact]
    public void Python_FormatsMaintainedOperationsAndIdentifiers()
    {
        var expression = Power(Variable("class"), FixedConstant(2.5))
            + AnalyticQuotient(Variable("sensor value"), Variable("scale"));

        expression.Build().ToPythonString().ShouldBe(
            "(math.pow(_class, 2.5) + (sensor_value / math.sqrt(1.0 + (scale * scale))))");
    }

    [Fact]
    public void CodeFormatters_PreserveMacros()
    {
        var expression = Sigmoid(Variable("x")).Build();

        expression.ToCSharpString().ShouldBe("sigmoid(x)");
        expression.ToPythonString().ShouldBe("sigmoid(x)");
    }

    [Fact]
    public void CodeFormatters_RepresentNonFiniteConstants()
    {
        FixedConstant(double.NaN).Build().ToCSharpString().ShouldBe("double.NaN");
        FixedConstant(double.PositiveInfinity).Build().ToPythonString().ShouldBe("math.inf");
    }

    [Fact]
    public void CodeFormatters_DisambiguateCollidingIdentifiers()
    {
        var expression = (Variable("class") + Variable("_class") + Variable("class")).Build();

        expression.ToCSharpString().ShouldBe("((_class + _class_2) + _class)");
        expression.ToPythonString().ShouldBe("((_class + _class_2) + _class)");
    }

    [Fact]
    public void Latex_FormatsTheImmutableExpressionTree()
    {
        var expression = (Square(Variable("sensor value")) + FixedConstant(2)).Build();

        ExpressionFormatters.Latex.Format(expression).ShouldBe(@"(\left(\mathrm{sensor\ value}\right)^2 + 2)");
        expression.ToLatexString().ShouldBe(@"(\left(\mathrm{sensor\ value}\right)^2 + 2)");
    }

    [Fact]
    public void Latex_PreservesMacros()
    {
        var expression = Sigmoid(Variable("x")).Build();

        ExpressionFormatters.Latex.Format(expression).ShouldBe(@"\operatorname{sigmoid}\left(\mathrm{x}\right)");
    }

    [Fact]
    public void Latex_EscapesNamesAndFormatsSpecialNumbers()
    {
        var expression = (Variable(@"sensor\_{a} %") + FixedConstant(1e20)).Build();

        expression.ToLatexString().ShouldBe(
            @"(\mathrm{sensor\backslash{}\_\{a\}\ \%} + 1 \times 10^{20})");
        FixedConstant(double.PositiveInfinity).Build().ToLatexString().ShouldBe(@"\infty");
    }
}
