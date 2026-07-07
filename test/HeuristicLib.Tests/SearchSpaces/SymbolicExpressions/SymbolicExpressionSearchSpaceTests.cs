using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.SearchSpaces.SymbolicExpressions;

public sealed class SymbolicExpressionSearchSpaceTests
{
    [Fact]
    public void Constructor_CreatesSearchSpaceWithBasicArithmeticOperations()
    {
        var searchSpace = new SymbolicExpressionSearchSpace(
            maximumLength: 10,
            maximumDepth: 5,
            allowedSymbols: Symbols.BasicArithmetic,
            allowedVariables: ["x0", "x1"]);

        searchSpace.AllowsVariables.ShouldBeTrue();
        searchSpace.AllowsNumericLiterals.ShouldBeTrue();
        searchSpace.AllowedTerminalSymbols.Select(symbol => symbol.GetType()).ShouldBe([
            typeof(VariableSymbol),
            typeof(NumericLiteralSymbol)
        ]);
        searchSpace.GetOperations(1).ShouldBe([
            new LogSymbol(),
            new SqrtSymbol()
        ]);
        searchSpace.GetOperations(2).ShouldBe([
            new AddSymbol(),
            new SubtractSymbol(),
            new MultiplySymbol(),
            new DivideSymbol()
        ]);
    }

    [Fact]
    public void ContainsOperation_UsesAllowedOperationSymbols()
    {
        var searchSpace = new SymbolicExpressionSearchSpace(
            maximumLength: 10,
            maximumDepth: 5,
            allowedSymbols: [new LogSymbol()],
            allowedVariables: ["x0"]);

        searchSpace.ContainsOperation(new LogSymbol()).ShouldBeTrue();
        searchSpace.ContainsOperation(new SqrtSymbol()).ShouldBeFalse();
        searchSpace.ContainsOperation(new VariableSymbol("x0")).ShouldBeFalse();
    }

    [Fact]
    public void Contains_AcceptsExpressionWithinStructuralLimits()
    {
        var searchSpace = new SymbolicExpressionSearchSpace(
            maximumLength: 5,
            maximumDepth: 3,
            allowedSymbols: Symbols.BasicArithmetic,
            allowedVariables: ["x0", "x1"]);
        var expression = CreateLinearExpression();

        searchSpace.Contains(expression).ShouldBeTrue();
    }

    [Fact]
    public void Contains_RejectsExpressionAboveMaximumLength()
    {
        var searchSpace = new SymbolicExpressionSearchSpace(
            maximumLength: 4,
            maximumDepth: 3,
            allowedSymbols: Symbols.BasicArithmetic,
            allowedVariables: ["x0", "x1"]);
        var expression = CreateLinearExpression();

        searchSpace.Contains(expression).ShouldBeFalse();
    }

    [Fact]
    public void Contains_RejectsExpressionAboveMaximumDepth()
    {
        var searchSpace = new SymbolicExpressionSearchSpace(
            maximumLength: 5,
            maximumDepth: 2,
            allowedSymbols: Symbols.BasicArithmetic,
            allowedVariables: ["x0", "x1"]);
        var expression = CreateLinearExpression();

        searchSpace.Contains(expression).ShouldBeFalse();
    }

    [Fact]
    public void Contains_RejectsDisallowedOperation()
    {
        var searchSpace = new SymbolicExpressionSearchSpace(
            maximumLength: 5,
            maximumDepth: 3,
            allowedSymbols: [new MultiplySymbol()],
            allowedVariables: ["x0", "x1"]);
        var expression = CreateLinearExpression();

        searchSpace.Contains(expression).ShouldBeFalse();
    }

    [Fact]
    public void Contains_RejectsDisallowedVariable()
    {
        var searchSpace = new SymbolicExpressionSearchSpace(
            maximumLength: 5,
            maximumDepth: 3,
            allowedSymbols: Symbols.BasicArithmetic,
            allowedVariables: ["x0"]);
        var expression = CreateLinearExpression();

        searchSpace.Contains(expression).ShouldBeFalse();
    }

    [Fact]
    public void Contains_AcceptsAllowedNumericLiteral()
    {
        var searchSpace = new SymbolicExpressionSearchSpace(
            maximumLength: 1,
            maximumDepth: 1,
            allowedSymbols: [],
            allowedVariables: []);
        var expression = Fixed(100.0).Build();

        searchSpace.Contains(expression).ShouldBeTrue();
    }

    [Fact]
    public void Contains_RejectsDisallowedNumericLiteral()
    {
        var searchSpace = new SymbolicExpressionSearchSpace(
            maximumLength: 1,
            maximumDepth: 1,
            allowedSymbols: [],
            allowedVariables: ["x0"],
            allowNumericLiterals: false);
        var expression = Fixed(100.0).Build();

        searchSpace.Contains(expression).ShouldBeFalse();
    }

    [Fact]
    public void Constructor_AllowsNoVariablesWhenNumericLiteralsAreAllowed()
    {
        var searchSpace = new SymbolicExpressionSearchSpace(
            maximumLength: 1,
            maximumDepth: 1,
            allowedSymbols: [],
            allowedVariables: []);

        searchSpace.AllowsVariables.ShouldBeFalse();
        searchSpace.AllowsNumericLiterals.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_DerivesVariableSymbolFromVariableNames()
    {
        var searchSpace = new SymbolicExpressionSearchSpace(
            maximumLength: 1,
            maximumDepth: 1,
            allowedSymbols: [],
            allowedVariables: ["x0"]);

        searchSpace.AllowsVariables.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_RejectsSearchSpaceWithoutTerminalSymbols()
    {
        Should.Throw<ArgumentException>(() => new SymbolicExpressionSearchSpace(
            maximumLength: 1,
            maximumDepth: 1,
            allowedSymbols: [new AddSymbol()],
            allowedVariables: [],
            allowNumericLiterals: false));
    }

    private static SymbolicExpression CreateLinearExpression()
    {
        return (Variable("x0") + Fixed(2.0) * Variable("x1")).Build();
    }

}
