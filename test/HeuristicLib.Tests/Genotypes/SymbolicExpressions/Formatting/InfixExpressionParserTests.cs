using System.Globalization;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Numerics;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;

namespace HEAL.HeuristicLib.Tests.Genotypes.SymbolicExpressions.Formatting;

public sealed class InfixExpressionParserTests
{
    [Fact]
    public void Parse_RespectsPrecedenceAndAssociativity()
    {
        var expression = InfixExpressionParser.Parse("x + 2 * y - z ^ 2 ^ 3");

        expression.ToInfixString().ShouldBe("((x + (2 * y)) - pow(z, pow(2, 3)))");
    }

    [Fact]
    public void Parse_HandlesUnaryOperatorsAndFunctions()
    {
        var expression = InfixExpressionParser.Parse("-x^2 + sqrt(abs(y))");

        expression.ToInfixString().ShouldBe("(negate(pow(x, 2)) + sqrt(abs(y)))");
    }

    [Fact]
    public void Parse_DistinguishesFixedAndEvolvableConstants()
    {
        var expression = InfixExpressionParser.Parse("2 + param(-3.5)");
        var constants = expression.TraversePreOrder().OfType<NumericConstantExpressionNode>().ToArray();

        constants[0].Symbol.ShouldBeOfType<FixedConstantSymbol>();
        constants[0].Value.ShouldBe(2.0);
        constants[1].Symbol.ShouldBeOfType<EvolvableConstantSymbol>();
        constants[1].Value.ShouldBe(-3.5);
    }

    [Fact]
    public void Parse_KeepsSignedAndNonFiniteLiteralsAsSingleConstantNodes()
    {
        var expression = InfixExpressionParser.Parse("-2 + param(-Infinity)");
        var constants = expression.TraversePreOrder().OfType<NumericConstantExpressionNode>().ToArray();

        constants.Length.ShouldBe(2);
        constants[0].Symbol.ShouldBeOfType<FixedConstantSymbol>();
        constants[0].Value.ShouldBe(-2.0);
        constants[1].Symbol.ShouldBeOfType<EvolvableConstantSymbol>();
        constants[1].Value.ShouldBe(double.NegativeInfinity);
        expression.ToInfixString(InfixConstantNotation.MarkAll)
            .ShouldBe("(fixed(-2) + param(-Infinity))");
    }

    [Fact]
    public void Parse_HandlesQuotedVariableNames()
    {
        var expression = InfixExpressionParser.Parse("`sensor value` + 1");

        expression.Root.ShouldBeOfType<BinaryExpressionNode>()
            .Left.ShouldBeOfType<VariableExpressionNode>()
            .VariableName.ShouldBe("sensor value");
        expression.ToInfixString().ShouldBe("(`sensor value` + 1)");
    }

    [Fact]
    public void Parse_RoundTripsEscapedVariableNamesFromTheInfixFormatter()
    {
        var original = ExpressionDraft.Variable("sensor `A`").Build();

        var parsed = InfixExpressionParser.Parse(original.ToInfixString());

        parsed.Root.ShouldBeOfType<VariableExpressionNode>()
            .VariableName.ShouldBe("sensor `A`");
        original.ToInfixString().ShouldBe("`sensor ``A```");
    }

    [Theory]
    [InlineData("nan")]
    [InlineData("Infinity")]
    public void Parse_TreatsQuotedSpecialNumbersAsVariableNames(string name)
    {
        var expression = InfixExpressionParser.Parse($"`{name}`");

        expression.Root.ShouldBeOfType<VariableExpressionNode>()
            .VariableName.ShouldBe(name);
    }

    [Theory]
    [InlineData("fixed")]
    [InlineData("param")]
    public void Parse_TreatsQuotedConstantMarkersAsCustomOperations(string name)
    {
        var operation = new CustomUnarySymbol(name);
        var variable = new VariableSymbol(["x"]);
        var searchSpace = new ExpressionTreeSearchSpace(3, 2, [operation, variable]);
        var expression = ExpressionDraft.Apply(operation, ExpressionDraft.Variable("x", variable)).Build(searchSpace);

        var formatted = expression.ToInfixString();
        var parsed = InfixExpressionParser.Parse(formatted, searchSpace);

        formatted.ShouldBe($"`{name}`(x)");
        parsed.ShouldBe(expression);
    }

    [Fact]
    public void Parse_BindsSymbolsFromTheSearchSpace()
    {
        var variable = new VariableSymbol(["x"]);
        var constant = new EvolvableConstantSymbol();
        var pi = new FixedConstantSymbol(Math.PI, "pi");
        var custom = new CustomUnarySymbol();
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 10,
            maximumDepth: 5,
            [Symbols.Addition, custom, variable, constant, pi]);

        var expression = InfixExpressionParser.Parse("custom(x) + pi + param(2)", searchSpace);

        expression.TraversePreOrder().Select(node => node.Symbol).ShouldBe([
            Symbols.Addition,
            Symbols.Addition,
            custom,
            variable,
            pi,
            constant
        ]);
    }

    [Fact]
    public void Parse_RejectsAnAmbiguousIdentifier()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 1,
            maximumDepth: 1,
            [new VariableSymbol(["pi"]), new FixedConstantSymbol(Math.PI, "pi")]);

        Should.Throw<FormatException>(() => InfixExpressionParser.Parse("pi", searchSpace));
    }

    [Fact]
    public void Parse_RejectsInvalidSyntaxAndUnknownFunctions()
    {
        Should.Throw<FormatException>(() => InfixExpressionParser.Parse("x +"));
        Should.Throw<FormatException>(() => InfixExpressionParser.Parse("unknown(x)"));
    }

    [Fact]
    public void Parse_ReportsTheSyntaxErrorLocationWithoutExposingParlot()
    {
        var exception = Should.Throw<FormatException>(() => InfixExpressionParser.Parse("x +\n* y"));

        exception.Message.ShouldContain("line 1");
        exception.Message.ShouldContain("column 2");
        exception.InnerException.ShouldBeNull();
    }

    [Fact]
    public void Parse_RejectsSymbolsThatAreNotInTheSearchSpace()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 3,
            maximumDepth: 2,
            [Symbols.Addition, new VariableSymbol(["x"])]);

        Should.Throw<FormatException>(() => InfixExpressionParser.Parse("sin(x)", searchSpace));
        Should.Throw<FormatException>(() => InfixExpressionParser.Parse("x + y", searchSpace));
    }

    [Fact]
    public void TryParse_ReturnsTheParsedExpressionOrFalse()
    {
        InfixExpressionParser.TryParse("x + 2", out var expression).ShouldBeTrue();
        expression.ToInfixString().ShouldBe("(x + 2)");

        InfixExpressionParser.TryParse("x +", out expression).ShouldBeFalse();
        expression.ShouldBeNull();
        InfixExpressionParser.TryParse("unknown(x)", out expression).ShouldBeFalse();
        expression.ShouldBeNull();
    }

    [Fact]
    public void TryParse_ReturnsFalseWhenTheExpressionCannotBindToTheSearchSpace()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 3,
            maximumDepth: 2,
            [Symbols.Addition, new VariableSymbol(["x"])]);

        InfixExpressionParser.TryParse("x + y", searchSpace, out var expression).ShouldBeFalse();
        expression.ShouldBeNull();
    }

    [Fact]
    public void TryParse_RejectsExpressionsOutsideTheSearchSpaceLimits()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 2,
            maximumDepth: 1,
            [Symbols.Addition, new VariableSymbol(["x"])]);

        InfixExpressionParser.TryParse("x + x", searchSpace, out var expression).ShouldBeFalse();
        expression.ShouldBeNull();
    }

    [Fact]
    public void TryParseDraft_ReturnsTheDraftOrFalse()
    {
        InfixExpressionParser.TryParseDraft("x + 2", out var draft).ShouldBeTrue();
        draft.Build().ToInfixString().ShouldBe("(x + 2)");

        InfixExpressionParser.TryParseDraft("x +", out draft).ShouldBeFalse();
        draft.ShouldBeNull();
    }

    [Fact]
    public void Parse_UsesTheConfiguredInterpretationForBareNumericLiterals()
    {
        var fixedExpression = InfixExpressionParser.Parse("2");
        var evolvableExpression = InfixExpressionParser.Parse(
            "2",
            NumericLiteralInterpretation.Evolvable);

        fixedExpression.Root.ShouldBeOfType<NumericConstantExpressionNode>()
            .Symbol.ShouldBeOfType<FixedConstantSymbol>();
        evolvableExpression.Root.ShouldBeOfType<NumericConstantExpressionNode>()
            .Symbol.ShouldBeOfType<EvolvableConstantSymbol>();
    }

    [Fact]
    public void Parse_ExplicitConstantKindsOverrideTheBareLiteralInterpretation()
    {
        var expression = InfixExpressionParser.Parse(
            "fixed(1) + param(2)",
            NumericLiteralInterpretation.Evolvable);
        var constants = expression.TraversePreOrder().OfType<NumericConstantExpressionNode>().ToArray();

        constants[0].Symbol.ShouldBeOfType<FixedConstantSymbol>();
        constants[1].Symbol.ShouldBeOfType<EvolvableConstantSymbol>();
    }

    [Fact]
    public void Parse_RoundTripsAnnotatedConstantKinds()
    {
        var expression = (ExpressionDraft.Constant(2) + ExpressionDraft.FixedConstant(3)).Build();

        InfixExpressionParser.Parse(
                expression.ToInfixString(InfixConstantNotation.MarkParameters))
            .ShouldBe(expression);
        InfixExpressionParser.Parse(
                expression.ToInfixString(InfixConstantNotation.MarkFixedConstants),
                NumericLiteralInterpretation.Evolvable)
            .ShouldBe(expression);
        InfixExpressionParser.Parse(
                expression.ToInfixString(InfixConstantNotation.MarkAll))
            .ShouldBe(expression);
    }

    [Theory]
    [InlineData("negate(x)")]
    [InlineData("exp(x)")]
    [InlineData("sin(x)")]
    [InlineData("cos(x)")]
    [InlineData("tan(x)")]
    [InlineData("tanh(x)")]
    [InlineData("log(x)")]
    [InlineData("sqrt(x)")]
    [InlineData("abs(x)")]
    [InlineData("square(x)")]
    [InlineData("cube(x)")]
    [InlineData("cbrt(x)")]
    [InlineData("pow(x, 2)")]
    [InlineData("root(x, 2)")]
    [InlineData("aq(x, y)")]
    [InlineData("sigmoid(x)")]
    public void Parse_RoundTripsMaintainedFunctions(string text)
    {
        var expression = InfixExpressionParser.Parse(text);

        InfixExpressionParser.Parse(expression.ToInfixString()).ShouldBe(expression);
    }

    [Fact]
    public void Parse_RoundTripsBacktickQuotedCustomOperationAndVariableNames()
    {
        var operation = new CustomUnarySymbol("moving average");
        var variable = new VariableSymbol(["sensor `A`"]);
        var searchSpace = new ExpressionTreeSearchSpace(3, 2, [operation, variable]);
        var expression = ExpressionDraft.Apply(operation, ExpressionDraft.Variable("sensor `A`", variable)).Build(searchSpace);

        var formatted = expression.ToInfixString();
        var parsed = InfixExpressionParser.Parse(formatted, searchSpace);

        formatted.ShouldBe("`moving average`(`sensor ``A```)");
        parsed.ShouldBe(expression);
    }

    [Fact]
    public void Parse_HandlesNumericEdgeCasesIndependentlyOfTheCurrentCulture()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-AT");
            var expression = InfixExpressionParser.Parse("1.25e-10 + fixed(-0)");
            var constants = expression.TraversePreOrder().OfType<NumericConstantExpressionNode>().ToArray();

            constants[0].Value.ShouldBe(1.25e-10);
            BitConverter.DoubleToInt64Bits(constants[1].Value).ShouldBe(BitConverter.DoubleToInt64Bits(-0.0));
            InfixExpressionParser.Parse(expression.ToInfixString(InfixConstantNotation.MarkAll))
                .ShouldBe(expression);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public void Parse_RejectsInvalidNumericLiteralInterpretation()
    {
        var invalid = (NumericLiteralInterpretation)int.MaxValue;

        Should.Throw<ArgumentOutOfRangeException>(() => InfixExpressionParser.Parse("x", invalid));
        Should.Throw<ArgumentOutOfRangeException>(() => InfixExpressionParser.TryParse("x", out _, invalid));
    }

    private sealed record CustomUnarySymbol(string OperationName = "custom") : OperationSymbol(OperationName, 1)
    {
        public override void Emit(ExpressionNode node, IExpressionEmitter emitter)
        {
            emitter.EmitChild(0);
            emitter.EmitOperation(Operation.Negate);
        }
    }
}
