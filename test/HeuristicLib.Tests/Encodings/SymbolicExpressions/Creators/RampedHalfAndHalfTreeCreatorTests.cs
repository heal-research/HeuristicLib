namespace HEAL.HeuristicLib.Tests.Operators.Creators.SymbolicExpressionCreators;

public sealed class RampedHalfAndHalfTreeCreatorTests
{
    [Fact]
    public void Create_PairsFullAndGrowTreesAcrossFeasibleDepths()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 15,
            maximumDepth: 4,
            operations: [Symbols.Addition],
            variables: ["x0"],
            constants: []);
        var creator = new RampedHalfAndHalfTreeCreator();

        var expressions = creator.Create(6, RandomNumberGenerator.Create(123), searchSpace);

        expressions.Count.ShouldBe(6);
        expressions[0].Depth.ShouldBe(2);
        expressions[0].Length.ShouldBe(3);
        expressions[1].Depth.ShouldBeLessThanOrEqualTo(2);
        expressions[2].Depth.ShouldBe(3);
        expressions[2].Length.ShouldBe(7);
        expressions[3].Depth.ShouldBeLessThanOrEqualTo(3);
        expressions[4].Depth.ShouldBe(4);
        expressions[4].Length.ShouldBe(15);
        expressions[5].Depth.ShouldBeLessThanOrEqualTo(4);
        expressions.All(searchSpace.Contains).ShouldBeTrue();
    }

    [Fact]
    public void Create_RepeatsTheRampForLargerPopulations()
    {
        var searchSpace = new ExpressionTreeSearchSpace(15, 4, [Symbols.Addition], ["x0"], constants: []);

        var expressions = new RampedHalfAndHalfTreeCreator().Create(
            12,
            RandomNumberGenerator.Create(123),
            searchSpace);

        expressions[6].Depth.ShouldBe(2);
        expressions[8].Depth.ShouldBe(3);
        expressions[10].Depth.ShouldBe(4);
        expressions.All(searchSpace.Contains).ShouldBeTrue();
    }

    [Fact]
    public void Create_UsesTheConfiguredDepthRange()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 31,
            maximumDepth: 5,
            operations: [Symbols.Addition],
            variables: ["x0"],
            constants: []);
        var creator = new RampedHalfAndHalfTreeCreator { MinimumDepth = 3, MaximumDepth = 4 };

        var expressions = creator.Create(4, RandomNumberGenerator.Create(123), searchSpace);

        expressions[0].Depth.ShouldBe(3);
        expressions[1].Depth.ShouldBeLessThanOrEqualTo(3);
        expressions[2].Depth.ShouldBe(4);
        expressions[3].Depth.ShouldBeLessThanOrEqualTo(4);
        expressions.All(expression => expression.Depth <= 4).ShouldBeTrue();
        expressions.All(searchSpace.Contains).ShouldBeTrue();
    }

    [Fact]
    public void Create_RejectsMaximumDepthBeyondTheSearchSpaceLimits()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 7,
            maximumDepth: 3,
            operations: [Symbols.Addition],
            variables: ["x0"],
            constants: []);
        var creator = new RampedHalfAndHalfTreeCreator { MinimumDepth = 2, MaximumDepth = 4 };

        Should.Throw<ArgumentException>(() =>
            creator.Create(5, RandomNumberGenerator.Create(123), searchSpace));
    }

    [Fact]
    public void Create_RejectsMinimumDepthBeyondTheDefaultMaximum()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 3,
            maximumDepth: 2,
            operations: [Symbols.Addition],
            variables: ["x0"],
            constants: []);
        var creator = new RampedHalfAndHalfTreeCreator { MinimumDepth = 3 };

        Should.Throw<ArgumentException>(() =>
            creator.Create(1, RandomNumberGenerator.Create(123), searchSpace));
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(2, 1)]
    public void Create_RejectsInvalidDepthRange(int minimumDepth, int? maximumDepth)
    {
        var searchSpace = new ExpressionTreeSearchSpace(15, 4, [Symbols.Addition], ["x0"], constants: []);
        var creator = new RampedHalfAndHalfTreeCreator { MinimumDepth = minimumDepth, MaximumDepth = maximumDepth };

        Should.Throw<InvalidOperationException>(() =>
            creator.Create(1, RandomNumberGenerator.Create(123), searchSpace));
    }

    [Fact]
    public void Create_HandlesTerminalOnlySearchSpaces()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 15,
            maximumDepth: 4,
            operations: Array.Empty<OperationSymbol>(),
            variables: ["x0"],
            constants: []);

        var expressions = new RampedHalfAndHalfTreeCreator().Create(
            4,
            RandomNumberGenerator.Create(123),
            searchSpace);

        expressions.Count.ShouldBe(4);
        expressions.All(expression => expression.Length == 1 && expression.Depth == 1).ShouldBeTrue();
        expressions.All(searchSpace.Contains).ShouldBeTrue();
    }
}
