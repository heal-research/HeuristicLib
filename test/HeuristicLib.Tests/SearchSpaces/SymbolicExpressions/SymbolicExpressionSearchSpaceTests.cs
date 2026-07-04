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
            allowedOperations: SymbolicExpressionOpCodes.BasicArithmetic,
            allowedVariables: ["x0", "x1"]);

        searchSpace.AllowsVariables.ShouldBeTrue();
        searchSpace.AllowsNumericLiterals.ShouldBeTrue();
        searchSpace.AllowedTerminalSymbols.ShouldBe([
            SymbolicExpressionOpCode.Variable,
            SymbolicExpressionOpCode.NumericLiteral
        ]);
        searchSpace.GetOperations(1).ShouldBe([
            SymbolicExpressionOpCode.Log,
            SymbolicExpressionOpCode.Sqrt
        ]);
        searchSpace.GetOperations(2).ShouldBe([
            SymbolicExpressionOpCode.Add,
            SymbolicExpressionOpCode.Subtract,
            SymbolicExpressionOpCode.Multiply,
            SymbolicExpressionOpCode.Divide
        ]);
    }

    [Fact]
    public void ContainsOperation_UsesCanonicalOperationArity()
    {
        var searchSpace = new SymbolicExpressionSearchSpace(
            maximumLength: 10,
            maximumDepth: 5,
            allowedOperations: [SymbolicExpressionOpCode.Log],
            allowedVariables: ["x0"]);

        searchSpace.ContainsOperation(SymbolicExpressionOpCode.Log, arity: 1).ShouldBeTrue();
        searchSpace.ContainsOperation(SymbolicExpressionOpCode.Log, arity: 2).ShouldBeFalse();
        searchSpace.ContainsOperation(SymbolicExpressionOpCode.Sqrt, arity: 1).ShouldBeFalse();
        searchSpace.ContainsOperation(SymbolicExpressionOpCode.Variable, arity: 0).ShouldBeFalse();
    }

    [Fact]
    public void Contains_AcceptsExpressionWithinStructuralLimits()
    {
        var searchSpace = new SymbolicExpressionSearchSpace(
            maximumLength: 5,
            maximumDepth: 3,
            allowedOperations: SymbolicExpressionOpCodes.BasicArithmetic,
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
            allowedOperations: SymbolicExpressionOpCodes.BasicArithmetic,
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
            allowedOperations: SymbolicExpressionOpCodes.BasicArithmetic,
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
            allowedOperations: [SymbolicExpressionOpCode.Multiply],
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
            allowedOperations: SymbolicExpressionOpCodes.BasicArithmetic,
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
            allowedOperations: [],
            allowedVariables: []);
        var expression = Fixed(100.0).Compile();

        searchSpace.Contains(expression).ShouldBeTrue();
    }

    [Fact]
    public void Contains_RejectsDisallowedNumericLiteral()
    {
        var searchSpace = new SymbolicExpressionSearchSpace(
            maximumLength: 1,
            maximumDepth: 1,
            allowedOperations: [],
            allowedVariables: ["x0"],
            allowNumericLiterals: false);
        var expression = Fixed(100.0).Compile();

        searchSpace.Contains(expression).ShouldBeFalse();
    }

    [Fact]
    public void Constructor_AllowsNoVariablesWhenNumericLiteralsAreAllowed()
    {
        var searchSpace = new SymbolicExpressionSearchSpace(
            maximumLength: 1,
            maximumDepth: 1,
            allowedOperations: [],
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
            allowedOperations: [],
            allowedVariables: ["x0"]);

        searchSpace.AllowsVariables.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_RejectsSearchSpaceWithoutTerminalSymbols()
    {
        Should.Throw<ArgumentException>(() => new SymbolicExpressionSearchSpace(
            maximumLength: 1,
            maximumDepth: 1,
            allowedOperations: [SymbolicExpressionOpCode.Add],
            allowedVariables: [],
            allowNumericLiterals: false));
    }

    private static SymbolicExpression CreateLinearExpression()
    {
        return (Variable("x0") + Fixed(2.0) * Variable("x1")).Compile();
    }

}
