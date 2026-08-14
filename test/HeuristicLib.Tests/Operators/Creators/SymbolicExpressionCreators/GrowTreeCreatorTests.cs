using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Operators.Creators.SymbolicExpressionCreators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;
using HEAL.HeuristicLib.Tests.TestSupport.Random;

namespace HEAL.HeuristicLib.Tests.Operators.Creators.SymbolicExpressionCreators;

public sealed class GrowTreeCreatorTests
{
    [Fact]
    public void Create_UsesSampledSymbolsAndRespectsStructuralBudgets()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 5,
            maximumDepth: 3,
            operations: [Symbols.Addition],
            variables: ["x0"],
            constants: []);
        var random = new SequenceRandomNumberGenerator(
            0.0,  // Addition at the root.
            0.99, // Allocate three nodes to the first child.
            0.0,  // Addition for the first child.
            0.0,  // First terminal.
            0.0,  // Second terminal.
            0.0); // Root's second terminal.

        var expression = GrowTreeCreation.Create(random, searchSpace);

        expression.ToInfixString().ShouldBe("((x0 + x0) + x0)");
        expression.Length.ShouldBe(5);
        expression.Depth.ShouldBe(3);
        searchSpace.Contains(expression).ShouldBeTrue();
    }

    [Fact]
    public void Create_ProducesOnlyATerminalAtMaximumDepthOne()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 31,
            maximumDepth: 1,
            operations: [Symbols.Addition],
            variables: ["x0"]);

        var expression = GrowTreeCreation.Create(RandomNumberGenerator.Create(123), searchSpace);

        expression.Root.ShouldBeAssignableTo<TerminalExpressionNode>();
        expression.Length.ShouldBe(1);
        expression.Depth.ShouldBe(1);
    }

    [Fact]
    public void CreatorInstance_UsesTheSearchSpaceSuppliedByTheAlgorithmContract()
    {
        var searchSpace = new ExpressionTreeSearchSpace(15, 4, [Symbols.Addition], ["x0"]);

        var expression = new GrowTreeCreator().CreateCandidate(RandomNumberGenerator.Create(123), searchSpace);

        searchSpace.Contains(expression).ShouldBeTrue();
    }

    [Fact]
    public void CreatorInstance_RestrictsCreationToTheConfiguredMaximumDepth()
    {
        var searchSpace = new ExpressionTreeSearchSpace(31, 5, [Symbols.Addition], ["x0"]);
        var creator = new GrowTreeCreator { MaximumDepth = 2 };

        var expression = creator.CreateCandidate(RandomNumberGenerator.Create(123), searchSpace);

        creator.MaximumDepth.ShouldBe(2);
        expression.Depth.ShouldBeLessThanOrEqualTo(2);
        searchSpace.Contains(expression).ShouldBeTrue();
    }

    [Fact]
    public void Create_RejectsNonPositiveMaximumDepth()
    {
        var searchSpace = new ExpressionTreeSearchSpace(15, 4, [Symbols.Addition], ["x0"], constants: []);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            new GrowTreeCreator { MaximumDepth = 0 }.CreateCandidate(RandomNumberGenerator.Create(123), searchSpace));
    }

    [Fact]
    public void CreatorInstance_RejectsMaximumDepthBeyondTheSearchSpaceLimit()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 4,
            maximumDepth: 2,
            operations: [Symbols.Negation],
            variables: ["x0"],
            constants: []);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            new GrowTreeCreator { MaximumDepth = 4 }.CreateCandidate(RandomNumberGenerator.Create(123), searchSpace));
    }

    [Theory]
    [InlineData(16, 4)]
    [InlineData(15, 5)]
    public void CreateSubtree_RejectsBudgetsBeyondTheSearchSpace(int maximumLength, int maximumDepth)
    {
        var searchSpace = new ExpressionTreeSearchSpace(15, 4, [Symbols.Addition], ["x0"]);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            GrowTreeCreation.CreateSubtree(
                RandomNumberGenerator.Create(123),
                searchSpace,
                maximumLength,
                maximumDepth));
    }
}
