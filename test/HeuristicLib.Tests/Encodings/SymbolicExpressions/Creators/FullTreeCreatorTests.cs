namespace HEAL.HeuristicLib.Tests.Operators.Creators.SymbolicExpressionCreators;

public sealed class FullTreeCreatorTests
{
    [Fact]
    public void Create_PlacesEveryLeafAtTheDeepestFeasibleDepth()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 15,
            maximumDepth: 6,
            operations: [Symbols.Addition],
            variables: ["x0"],
            constants: []);

        var expression = FullTreeCreation.Create(RandomNumberGenerator.Create(123), searchSpace);

        expression.Length.ShouldBe(15);
        expression.Depth.ShouldBe(4);
        AssertFullAtDepth(expression.Root, expression.Depth);
        searchSpace.Contains(expression).ShouldBeTrue();
    }

    [Fact]
    public void Create_UsesFeasibleAritiesWhenUnaryAndBinaryOperationsAreAvailable()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 5,
            maximumDepth: 4,
            operations: [Symbols.Addition, Symbols.Negation],
            variables: ["x0"],
            constants: []);

        var expression = new FullTreeCreator().CreateCandidate(RandomNumberGenerator.Create(123), searchSpace);

        expression.Length.ShouldBeLessThanOrEqualTo(5);
        expression.Depth.ShouldBe(4);
        AssertFullAtDepth(expression.Root, expression.Depth);
        searchSpace.Contains(expression).ShouldBeTrue();
    }

    [Fact]
    public void CreatorInstance_UsesTheConfiguredExactDepth()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 31,
            maximumDepth: 5,
            operations: [Symbols.Addition],
            variables: ["x0"],
            constants: []);

        var expression = new FullTreeCreator { Depth = 3 }.CreateCandidate(RandomNumberGenerator.Create(123), searchSpace);

        expression.Depth.ShouldBe(3);
        expression.Length.ShouldBe(7);
        AssertFullAtDepth(expression.Root, remainingDepth: 3);
    }

    [Fact]
    public void Create_RejectsNonPositiveDepth()
    {
        var searchSpace = new ExpressionTreeSearchSpace(15, 4, [Symbols.Addition], ["x0"], constants: []);

        Should.Throw<InvalidOperationException>(() =>
            new FullTreeCreator { Depth = 0 }.CreateCandidate(RandomNumberGenerator.Create(123), searchSpace));
    }

    [Fact]
    public void CreatorInstance_RejectsDepthBeyondTheSearchSpaceMaximum()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 3,
            maximumDepth: 2,
            operations: [Symbols.Addition],
            variables: ["x0"],
            constants: []);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            new FullTreeCreator { Depth = 3 }.CreateCandidate(RandomNumberGenerator.Create(123), searchSpace));
    }

    [Fact]
    public void CreatorInstance_RejectsDepthThatCannotFitWithinTheSearchSpaceLength()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 3,
            maximumDepth: 3,
            operations: [Symbols.Addition],
            variables: ["x0"],
            constants: []);

        Should.Throw<ArgumentException>(() =>
            new FullTreeCreator { Depth = 3 }.CreateCandidate(RandomNumberGenerator.Create(123), searchSpace));
    }

    [Fact]
    public void CreateSubtreeAtDepth_RejectsAnInfeasibleLengthBudget()
    {
        var searchSpace = new ExpressionTreeSearchSpace(15, 4, [Symbols.Addition], ["x0"], constants: []);

        Should.Throw<ArgumentException>(() =>
            FullTreeCreation.CreateSubtreeAtDepth(
                RandomNumberGenerator.Create(123),
                searchSpace,
                maximumLength: 6,
                depth: 3));
    }

    [Theory]
    [InlineData(16, 4)]
    [InlineData(15, 5)]
    public void CreateSubtreeAtDepth_RejectsBudgetsBeyondTheSearchSpace(int maximumLength, int depth)
    {
        var searchSpace = new ExpressionTreeSearchSpace(15, 4, [Symbols.Addition], ["x0"], constants: []);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            FullTreeCreation.CreateSubtreeAtDepth(
                RandomNumberGenerator.Create(123),
                searchSpace,
                maximumLength,
                depth));
    }

    [Fact]
    public void Create_ReturnsATerminalWhenNoOperationsAreAvailable()
    {
        var searchSpace = new ExpressionTreeSearchSpace(15, 4, Array.Empty<OperationSymbol>(), ["x0"], constants: []);

        var expression = FullTreeCreation.Create(RandomNumberGenerator.Create(123), searchSpace);

        expression.Root.ShouldBeOfType<VariableExpressionNode>();
        expression.Length.ShouldBe(1);
        expression.Depth.ShouldBe(1);
    }

    private static void AssertFullAtDepth(ExpressionNode node, int remainingDepth)
    {
        if (remainingDepth == 1)
        {
            node.ShouldBeAssignableTo<TerminalExpressionNode>();
            return;
        }

        node.ShouldBeAssignableTo<OperationExpressionNode>();
        for (var i = 0; i < node.Arity; i++)
            AssertFullAtDepth(node.GetChild(i), remainingDepth - 1);
    }
}
