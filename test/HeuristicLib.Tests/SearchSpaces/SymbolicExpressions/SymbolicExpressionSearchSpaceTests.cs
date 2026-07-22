using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Random.Distributions;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;
using HEAL.HeuristicLib.Tests.TestSupport.Random;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.SearchSpaces.SymbolicExpressions;

public sealed class SymbolicExpressionSearchSpaceTests
{
    [Fact]
    public void Constructor_AddsDefaultEvolvableConstantForCommonAuthoring()
    {
        var searchSpace = new ExpressionTreeSearchSpace(10, 5, Symbols.BasicArithmetic, ["x0", "x1"]);

        searchSpace.Symbols.OfType<VariableSymbol>().Single().Variables.ShouldBe(["x0", "x1"]);
        searchSpace.Symbols.OfType<EvolvableConstantSymbol>().ShouldHaveSingleItem();
        searchSpace.GetSymbols(2).ShouldBe([new AdditionSymbol(), new SubtractionSymbol(), new MultiplicationSymbol(), new DivisionSymbol()]);
    }

    [Fact]
    public void Contains_IgnoresEvolvableSamplingGuidanceButChecksFixedConstantIdentity()
    {
        var allowed = new EvolvableConstantSymbol(new UniformDoubleDistribution(-1, 1), new ResampleInitialNumericPerturbation());
        var searchSpace = new ExpressionTreeSearchSpace(1, 1, [allowed, new FixedConstantSymbol(Math.PI, "pi")]);

        searchSpace.Contains(Constant(100.0, new EvolvableConstantSymbol(new UniformDoubleDistribution(100, 200), new AdditiveNumericPerturbation(new NormalDoubleDistribution(0, 1)))).Build()).ShouldBeTrue();
        searchSpace.Contains(FixedConstant(Math.PI).Build()).ShouldBeFalse();
        searchSpace.Contains(FixedConstant(Math.PI, "pi").Build()).ShouldBeTrue();
    }

    [Fact]
    public void Contains_AcceptsExpressionsWithinStructuralLimits()
    {
        var searchSpace = new ExpressionTreeSearchSpace(5, 3, Symbols.BasicArithmetic, ["x0", "x1"], [new FixedConstantSymbol(2.0)]);

        searchSpace.Contains(CreateLinearExpression()).ShouldBeTrue();
    }

    [Fact]
    public void Contains_RejectsExpressionsAboveLengthOrDepthLimits()
    {
        var expression = CreateLinearExpression();

        new ExpressionTreeSearchSpace(4, 3, Symbols.BasicArithmetic, ["x0", "x1"], [new FixedConstantSymbol(2.0)]).Contains(expression).ShouldBeFalse();
        new ExpressionTreeSearchSpace(5, 2, Symbols.BasicArithmetic, ["x0", "x1"], [new FixedConstantSymbol(2.0)]).Contains(expression).ShouldBeFalse();
    }

    [Fact]
    public void Contains_RejectsExpressionsAboveTheMaximumLength()
    {
        new ExpressionTreeSearchSpace(4, 3, Symbols.BasicArithmetic, ["x0", "x1"], [new FixedConstantSymbol(2.0)])
            .Contains(CreateLinearExpression()).ShouldBeFalse();
    }

    [Fact]
    public void Contains_RejectsExpressionsAboveTheMaximumDepth()
    {
        new ExpressionTreeSearchSpace(5, 2, Symbols.BasicArithmetic, ["x0", "x1"], [new FixedConstantSymbol(2.0)])
            .Contains(CreateLinearExpression()).ShouldBeFalse();
    }

    [Fact]
    public void Contains_RejectsDisallowedOperationsAndVariables()
    {
        var expression = CreateLinearExpression();
        var operations = new ExpressionTreeSearchSpace(5, 3, [Symbols.Multiplication, new VariableSymbol(["x0", "x1"]), new FixedConstantSymbol(2.0)]);
        var variables = new ExpressionTreeSearchSpace(5, 3, Symbols.BasicArithmetic, ["x0"], [new FixedConstantSymbol(2.0)]);

        operations.Contains(expression).ShouldBeFalse();
        variables.Contains(expression).ShouldBeFalse();
    }

    [Fact]
    public void Contains_RejectsDisallowedOperations()
    {
        var searchSpace = new ExpressionTreeSearchSpace(5, 3, [Symbols.Multiplication, new VariableSymbol(["x0", "x1"]), new FixedConstantSymbol(2.0)]);

        searchSpace.Contains(CreateLinearExpression()).ShouldBeFalse();
    }

    [Fact]
    public void Contains_RejectsDisallowedVariables()
    {
        var searchSpace = new ExpressionTreeSearchSpace(5, 3, Symbols.BasicArithmetic, ["x0"], [new FixedConstantSymbol(2.0)]);

        searchSpace.Contains(CreateLinearExpression()).ShouldBeFalse();
    }

    [Fact]
    public void Contains_AcceptsEvolvableConstantsOnlyWhenThatTerminalFamilyIsPresent()
    {
        var evolvable = Constant(100.0).Build();
        var allowed = new ExpressionTreeSearchSpace(1, 1, [new EvolvableConstantSymbol()]);
        var disallowed = new ExpressionTreeSearchSpace(1, 1, [new VariableSymbol(["x0"])]);

        allowed.Contains(evolvable).ShouldBeTrue();
        disallowed.Contains(evolvable).ShouldBeFalse();
        allowed.AllowsVariables.ShouldBeFalse();
        allowed.AllowsEvolvableConstants.ShouldBeTrue();
    }

    [Fact]
    public void Contains_AcceptsAnAllowedPayloadlessTerminal()
    {
        var symbol = new TestPayloadlessTerminalSymbol();
        var searchSpace = new ExpressionTreeSearchSpace(1, 1, [symbol]);
        var expression = new ExpressionTree(new PayloadlessTerminalExpressionNode(symbol));

        searchSpace.Contains(expression).ShouldBeTrue();
    }

    [Fact]
    public void Constructor_ExposesTerminalFamilyCapabilities()
    {
        var constants = new ExpressionTreeSearchSpace(1, 1, [new EvolvableConstantSymbol()]);
        var variables = new ExpressionTreeSearchSpace(1, 1, [new VariableSymbol(["x0"])]);

        constants.AllowsVariables.ShouldBeFalse();
        constants.AllowsEvolvableConstants.ShouldBeTrue();
        variables.AllowsVariables.ShouldBeTrue();
        variables.AllowsEvolvableConstants.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_RequiresAtLeastOneTerminalSymbol()
    {
        Should.Throw<ArgumentException>(() => new ExpressionTreeSearchSpace(1, 1, [Symbols.Addition]));
    }

    [Fact]
    public void WeightedSymbols_UseTheSearchSpaceSelectionWeights()
    {
        var searchSpace = new ExpressionTreeSearchSpace(1, 1,
        [
            (new FixedConstantSymbol(1.0), 1.0),
            (new FixedConstantSymbol(2.0), 3.0)
        ]);

        var symbol = searchSpace.SelectSymbol(0, new SequenceRandomNumberGenerator(0.9));
        symbol.CreateNode(new SequenceRandomNumberGenerator(0.9)).ShouldBeOfType<NumericConstantExpressionNode>().Value.ShouldBe(2.0);
    }

    [Fact]
    public void Equality_UsesOnlyCanonicalConfigurationValues()
    {
        var first = new ExpressionTreeSearchSpace(10, 5, Symbols.BasicArithmetic, ["x0", "x1"]);
        var second = new ExpressionTreeSearchSpace(10, 5, Symbols.BasicArithmetic, ["x0", "x1"]);

        first.ShouldBe(second);
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    [Fact]
    public void Equality_IncludesSymbolSelectionWeights()
    {
        Symbol[] symbols = [Symbols.Addition, new VariableSymbol(["x0"])];
        var first = new ExpressionTreeSearchSpace(10, 5, symbols, [1.0, 2.0]);
        var second = new ExpressionTreeSearchSpace(10, 5, symbols, [1.0, 3.0]);

        first.ShouldNotBe(second);
    }

    [Fact]
    public void Constructor_RetainsDuplicateSymbolEntries()
    {
        var searchSpace = new ExpressionTreeSearchSpace(10, 5, [Symbols.Addition, Symbols.Addition, new VariableSymbol(["x0"])]);

        searchSpace.Symbols.ShouldBe([Symbols.Addition, Symbols.Addition, new VariableSymbol(["x0"])]);
        searchSpace.GetSymbols(2).ShouldBe([Symbols.Addition, Symbols.Addition]);
    }

    private static ExpressionTree CreateLinearExpression() =>
        (Variable("x0") + FixedConstant(2.0) * Variable("x1")).Build();

    private sealed record TestPayloadlessTerminalSymbol() : PayloadlessTerminalSymbol("terminal")
    {
        public override void Emit(ExpressionNode node, IExpressionEmitter emitter)
        {
            emitter.EmitConstant(0.0);
        }
    }
}
