using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Operators.Creators.SymbolicExpressionCreators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Random.Distributions;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;
using HEAL.HeuristicLib.Tests.TestSupport.Random;

namespace HEAL.HeuristicLib.Tests.Operators.Creators.SymbolicExpressionCreators;

public sealed class BalancedTreeCreatorTests
{
    [Fact]
    public void Create_ExpandsEveryShallowerPositionBeforeTheNextLevel()
    {
        var searchSpace = CreateBinarySearchSpace(maximumLength: 7, maximumDepth: 4);

        var expression = BalancedTreeCreation.Create(
            RandomNumberGenerator.Create(123),
            searchSpace,
            requestedLength: 7);

        expression.Length.ShouldBe(7);
        expression.Depth.ShouldBe(3);
        expression.Root.ShouldBeOfType<BinaryExpressionNode>();
        expression.Root.GetChild(0).ShouldBeOfType<BinaryExpressionNode>();
        expression.Root.GetChild(1).ShouldBeOfType<BinaryExpressionNode>();
        searchSpace.Contains(expression).ShouldBeTrue();
    }

    [Fact]
    public void Create_DistributesAnIncompleteLevelAcrossShallowPositions()
    {
        var searchSpace = CreateBinarySearchSpace(maximumLength: 5, maximumDepth: 4);

        var expression = BalancedTreeCreation.Create(
            RandomNumberGenerator.Create(123),
            searchSpace,
            requestedLength: 5);

        var root = expression.Root.ShouldBeOfType<BinaryExpressionNode>();
        var operationChildren = new[] { root.Left, root.Right }
            .Count(child => child is BinaryExpressionNode);

        expression.Length.ShouldBe(5);
        expression.Depth.ShouldBe(3);
        operationChildren.ShouldBe(1);
        searchSpace.Contains(expression).ShouldBeTrue();
    }

    [Fact]
    public void Create_IrregularityAllowsDeeperShapes()
    {
        var searchSpace = CreateBinarySearchSpace(maximumLength: 7, maximumDepth: 4);
        var regular = BalancedTreeCreation.Create(
            RandomNumberGenerator.Create(123),
            searchSpace,
            requestedLength: 7,
            irregularity: 0.0);
        var irregular = BalancedTreeCreation.Create(
            new SequenceRandomNumberGenerator(Enumerable.Repeat(0.99, 20).ToArray()),
            searchSpace,
            requestedLength: 7,
            irregularity: 1.0);

        regular.Depth.ShouldBe(3);
        irregular.Length.ShouldBe(7);
        irregular.Depth.ShouldBe(4);
        searchSpace.Contains(irregular).ShouldBeTrue();
    }

    [Fact]
    public void Create_DoesNotExceedTheSearchSpaceWhenArityCannotReachTheRequestedLength()
    {
        var searchSpace = CreateBinarySearchSpace(maximumLength: 4, maximumDepth: 3);

        var expression = BalancedTreeCreation.Create(
            RandomNumberGenerator.Create(123),
            searchSpace,
            requestedLength: 4);

        expression.Length.ShouldBe(3);
        searchSpace.Contains(expression).ShouldBeTrue();
    }

    [Fact]
    public void Create_StopsExpandingAtTheMaximumDepth()
    {
        var searchSpace = CreateBinarySearchSpace(maximumLength: 15, maximumDepth: 2);

        var expression = BalancedTreeCreation.Create(
            RandomNumberGenerator.Create(123),
            searchSpace,
            requestedLength: 15);

        expression.Length.ShouldBe(3);
        expression.Depth.ShouldBe(2);
        searchSpace.Contains(expression).ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsATerminalWhenNoOperationsAreAvailable()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 15,
            maximumDepth: 4,
            operations: [],
            variables: ["x0"],
            constants: []);

        var expression = BalancedTreeCreation.Create(
            RandomNumberGenerator.Create(123),
            searchSpace,
            requestedLength: 15);

        expression.Root.ShouldBeOfType<VariableExpressionNode>();
        expression.Length.ShouldBe(1);
    }

    [Fact]
    public void Create_UsesUnaryOperationsToReachEvenRequestedLengths()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 4,
            maximumDepth: 4,
            operations: [Symbols.Negation],
            variables: ["x0"],
            constants: []);

        var expression = BalancedTreeCreation.Create(
            RandomNumberGenerator.Create(123),
            searchSpace,
            requestedLength: 4);

        expression.Length.ShouldBe(4);
        expression.Depth.ShouldBe(4);
        searchSpace.Contains(expression).ShouldBeTrue();
    }

    [Fact]
    public void CreatorInstance_UsesItsConfiguredLengthDistributionAndIrregularity()
    {
        var searchSpace = CreateBinarySearchSpace(maximumLength: 7, maximumDepth: 4);
        var creator = new BalancedTreeCreator(
            irregularity: 0.0,
            requestedLengthDistribution: new FixedDistribution<int>(7));

        var expression = creator.Create(RandomNumberGenerator.Create(123), searchSpace);

        creator.Irregularity.ShouldBe(0.0);
        expression.Length.ShouldBe(7);
        expression.Depth.ShouldBe(3);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void Constructor_RejectsInvalidIrregularity(double irregularity)
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new BalancedTreeCreator(irregularity));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    public void Create_RejectsRequestedLengthsOutsideTheAvailableBudget(int requestedLength)
    {
        var searchSpace = CreateBinarySearchSpace(maximumLength: 7, maximumDepth: 3);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            BalancedTreeCreation.Create(
                RandomNumberGenerator.Create(123),
                searchSpace,
                requestedLength));
    }

    [Theory]
    [InlineData(8, 3)]
    [InlineData(7, 4)]
    public void CreateSubtree_RejectsBudgetsBeyondTheSearchSpace(int maximumLength, int maximumDepth)
    {
        var searchSpace = CreateBinarySearchSpace(maximumLength: 7, maximumDepth: 3);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            BalancedTreeCreation.CreateSubtree(
                RandomNumberGenerator.Create(123),
                searchSpace,
                maximumLength,
                maximumDepth));
    }

    private static ExpressionTreeSearchSpace CreateBinarySearchSpace(int maximumLength, int maximumDepth)
    {
        return new ExpressionTreeSearchSpace(
            maximumLength,
            maximumDepth,
            operations: [Symbols.Addition],
            variables: ["x0"],
            constants: []);
    }

    private sealed record FixedDistribution<T>(T Value) : IDistribution<T>
    {
        public T Sample(IRandomNumberGenerator random)
        {
            return Value;
        }
    }
}
