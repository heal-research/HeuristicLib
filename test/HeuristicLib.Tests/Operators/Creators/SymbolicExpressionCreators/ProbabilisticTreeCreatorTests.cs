using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Operators.Creators.SymbolicExpressionCreators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Random.Distributions;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;
using HEAL.HeuristicLib.Tests.TestSupport.Random;

namespace HEAL.HeuristicLib.Tests.Operators.Creators.SymbolicExpressionCreators;

public sealed class ProbabilisticTreeCreatorTests
{
    [Fact]
    public void Create_ExpandsRandomFrontierPositionsUntilTheRequestedLengthIsReached()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 5,
            maximumDepth: 3,
            operations: [Symbols.Addition],
            variables: ["x0"],
            constants: []);
        // Selecting from singleton symbol sets is deterministic and does not consume random values.
        var random = new SequenceRandomNumberGenerator(
            0.99, // Expand the root's right child.
            0.0,  // Complete the remaining frontier positions.
            0.0,
            0.0);

        var expression = ProbabilisticTreeCreation.Create(random, searchSpace, requestedLength: 5);

        expression.ToInfixString().ShouldBe("(x0 + (x0 + x0))");
        expression.Length.ShouldBe(5);
        expression.Depth.ShouldBe(3);
        searchSpace.Contains(expression).ShouldBeTrue();
    }

    [Fact]
    public void Create_AllowsTheOriginalPtc2ArityOvershoot()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 5,
            maximumDepth: 3,
            operations: [Symbols.Addition],
            variables: ["x0"],
            constants: []);

        var expression = ProbabilisticTreeCreation.Create(
            RandomNumberGenerator.Create(123),
            searchSpace,
            requestedLength: 4);

        expression.Length.ShouldBe(5);
        searchSpace.Contains(expression).ShouldBeTrue();
    }

    [Fact]
    public void Create_DoesNotExceedTheSearchSpaceWhenArityCannotReachTheRequestedLength()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 4,
            maximumDepth: 3,
            operations: [Symbols.Addition],
            variables: ["x0"],
            constants: []);

        var expression = ProbabilisticTreeCreation.Create(
            RandomNumberGenerator.Create(123),
            searchSpace,
            requestedLength: 4);

        expression.Length.ShouldBe(3);
        searchSpace.Contains(expression).ShouldBeTrue();
    }

    [Fact]
    public void Create_StopsExpandingAtTheMaximumDepth()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 15,
            maximumDepth: 2,
            operations: [Symbols.Addition],
            variables: ["x0"],
            constants: []);

        var expression = ProbabilisticTreeCreation.Create(
            RandomNumberGenerator.Create(123),
            searchSpace,
            requestedLength: 15);

        expression.Length.ShouldBe(3);
        expression.Depth.ShouldBe(2);
        searchSpace.Contains(expression).ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsATerminalForRequestedLengthOne()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 15,
            maximumDepth: 4,
            operations: [Symbols.Addition],
            variables: ["x0"],
            constants: []);

        var expression = ProbabilisticTreeCreation.Create(
            RandomNumberGenerator.Create(123),
            searchSpace,
            requestedLength: 1);

        expression.Root.ShouldBeOfType<VariableExpressionNode>();
        expression.Length.ShouldBe(1);
    }

    [Fact]
    public void CreatorInstance_UsesTheConfiguredRequestedLengthDistribution()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 7,
            maximumDepth: 3,
            operations: [Symbols.Addition],
            variables: ["x0"],
            constants: []);
        var creator = new ProbabilisticTreeCreator { RequestedLengthDistribution = new FixedDistribution<int>(7) };

        var expression = creator.CreateCandidate(RandomNumberGenerator.Create(123), searchSpace);

        expression.Length.ShouldBe(7);
        searchSpace.Contains(expression).ShouldBeTrue();
    }

    [Fact]
    public void CreatorInstance_UsesUniformRequestedLengthsByDefault()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 7,
            maximumDepth: 3,
            operations: [Symbols.Addition],
            variables: ["x0"],
            constants: []);
        var random = new SequenceRandomNumberGenerator(
            0.0, // Uniformly request length one.
            0.0, // Select the variable symbol.
            0.0); // Select x0.

        var expression = new ProbabilisticTreeCreator().CreateCandidate(random, searchSpace);

        expression.Length.ShouldBe(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    public void Create_RejectsRequestedLengthsOutsideTheAvailableBudget(int requestedLength)
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 7,
            maximumDepth: 3,
            operations: [Symbols.Addition],
            variables: ["x0"],
            constants: []);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            ProbabilisticTreeCreation.Create(
                RandomNumberGenerator.Create(123),
                searchSpace,
                requestedLength));
    }

    [Theory]
    [InlineData(8, 3)]
    [InlineData(7, 4)]
    public void CreateSubtree_RejectsBudgetsBeyondTheSearchSpace(int maximumLength, int maximumDepth)
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 7,
            maximumDepth: 3,
            operations: [Symbols.Addition],
            variables: ["x0"],
            constants: []);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            ProbabilisticTreeCreation.CreateSubtree(
                RandomNumberGenerator.Create(123),
                searchSpace,
                maximumLength,
                maximumDepth));
    }

    private sealed record FixedDistribution<T>(T Value) : IDistribution<T>
    {
        public T Sample(IRandomNumberGenerator random)
        {
            return Value;
        }
    }
}
