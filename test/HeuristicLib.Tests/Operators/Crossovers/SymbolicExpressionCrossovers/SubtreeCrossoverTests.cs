using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Operators.Crossovers.SymbolicExpressionCrossovers;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;
using HEAL.HeuristicLib.Tests.TestSupport.Random;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.Operators.Crossovers.SymbolicExpressionCrossovers;

public sealed class SubtreeCrossoverTests
{
    [Fact]
    public void Cross_PreservesAggregateContainmentForNonCanonicalVariableOrigins()
    {
        var origin = new VariableSymbol(["x0", "x1"]);
        var parent1 = new ExpressionTree(new VariableExpressionNode(origin, "x0"));
        var parent2 = new ExpressionTree(new VariableExpressionNode(origin, "x1"));
        var searchSpace = new ExpressionTreeSearchSpace(1, 1,
        [
            new VariableSymbol(["x0"]),
            new VariableSymbol(["x1"])
        ]);

        var offspring = SubtreeCrossover.Cross(
            parent1,
            parent2,
            new SequenceRandomNumberGenerator(0.0),
            searchSpace);

        searchSpace.Contains(parent1).ShouldBeTrue();
        searchSpace.Contains(parent2).ShouldBeTrue();
        offspring.ToInfixString().ShouldBe("x1");
        searchSpace.Contains(offspring).ShouldBeTrue();
    }

    [Fact]
    public void Cross_SelectsTheConfiguredDestinationAndReservoirSampledDonor()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 9,
            maximumDepth: 4,
            operations: [Symbols.Addition, Symbols.Multiplication],
            variables: ["x0", "x1"],
            constants: [new FixedConstantSymbol(2.0)]);
        var parent1 = (Variable("x0") + FixedConstant(2.0) * Variable("x1")).Build(searchSpace);
        var parent2 = ((Variable("x1") + Variable("x0")) * FixedConstant(2.0)).Build(searchSpace);
        var parent1Root = parent1.Root.ShouldBeOfType<BinaryExpressionNode>();
        var parent2Root = parent2.Root.ShouldBeOfType<BinaryExpressionNode>();
        var donor = parent2Root.Left.ShouldBeOfType<BinaryExpressionNode>().Right;
        var random = new SequenceRandomNumberGenerator(
            0.5,  // Destination: parent1's multiplication subtree at preorder index 2.
            0.75, // Keep donor candidate 1 when candidate 2 is visited.
            0.5,  // Keep donor candidate 1 when candidate 3 is visited.
            0.0,  // Select donor candidate 4: parent2's x0 node.
            0.5); // Keep donor candidate 4 when candidate 5 is visited.

        var offspring = SubtreeCrossover.Cross(
            parent1,
            parent2,
            random,
            searchSpace);

        offspring.ToInfixString().ShouldBe("(x0 + x0)");
        searchSpace.Contains(offspring).ShouldBeTrue();
        parent1.ToInfixString().ShouldBe("(x0 + (2 * x1))");
        parent2.ToInfixString().ShouldBe("((x1 + x0) * 2)");
        var offspringRoot = offspring.Root.ShouldBeOfType<BinaryExpressionNode>();
        offspringRoot.Left.ShouldBeSameAs(parent1Root.Left);
        offspringRoot.Right.ShouldBeSameAs(donor);
    }

    [Fact]
    public void Cross_RejectsDonorBranchesThatWouldExceedTheLimits()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 1,
            maximumDepth: 1,
            operations: [Symbols.Addition],
            variables: ["x0", "x1"]);
        var parent1 = Variable("x1").Build(searchSpace);
        var unrestricted = new ExpressionTreeSearchSpace(7, 3, [Symbols.Addition], ["x0", "x1"]);
        var parent2 = (Variable("x0") + Variable("x1")).Build(unrestricted);
        var random = new SequenceRandomNumberGenerator(
            0.0,  // The only destination node.
            0.75); // Keep x0 when the second valid terminal donor is visited.

        var offspring = SubtreeCrossover.Cross(
            parent1,
            parent2,
            random,
            searchSpace);

        offspring.ToInfixString().ShouldBe("x0");
        offspring.Length.ShouldBe(1);
        offspring.Depth.ShouldBe(1);
        searchSpace.Contains(offspring).ShouldBeTrue();
    }

    [Fact]
    public void Cross_RejectsDonorBranchThatWouldOnlyExceedTheDepthLimit()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 5,
            maximumDepth: 2,
            operations: [Symbols.Addition],
            variables: ["x0", "x1", "x2", "x3"]);
        var parent1 = (Variable("x0") + Variable("x1")).Build(searchSpace);
        var parent2 = (Variable("x2") + Variable("x3")).Build(searchSpace);
        var random = new SequenceRandomNumberGenerator(
            0.9,  // Select parent1's x1 terminal at preorder index 2.
            0.75); // Retain x2 when x3 becomes the second valid donor.

        var offspring = SubtreeCrossover.Cross(
            parent1,
            parent2,
            random,
            searchSpace);

        offspring.ToInfixString().ShouldBe("(x0 + x2)");
        offspring.Length.ShouldBe(3);
        offspring.Depth.ShouldBe(2);
        searchSpace.Contains(offspring).ShouldBeTrue();
    }

    [Fact]
    public void Cross_WithInternalNodeProbabilityOne_SelectsInternalNodes()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 7,
            maximumDepth: 3,
            operations: [Symbols.Addition, Symbols.Multiplication],
            variables: ["x0", "x1", "x2", "x3"]);
        var parent1 = (Variable("x0") + Variable("x1")).Build(searchSpace);
        var parent2 = (Variable("x2") * Variable("x3")).Build(searchSpace);

        var offspring = SubtreeCrossover.Cross(
            parent1,
            parent2,
            RandomNumberGenerator.Create(123),
            searchSpace,
            internalNodeProbability: 1.0);

        offspring.ShouldBe(parent2);
    }

    [Fact]
    public void Cross_WithInternalNodeProbabilityZero_SelectsTerminalNodes()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 7,
            maximumDepth: 3,
            operations: [Symbols.Addition, Symbols.Multiplication],
            variables: ["x0", "x1", "x2", "x3"]);
        var parent1 = (Variable("x0") + Variable("x1")).Build(searchSpace);
        var parent2 = (Variable("x2") * Variable("x3")).Build(searchSpace);

        var offspring = SubtreeCrossover.Cross(
            parent1,
            parent2,
            RandomNumberGenerator.Create(123),
            searchSpace,
            internalNodeProbability: 0.0);

        offspring.Root.Symbol.ShouldBe(Symbols.Addition);
        offspring.Length.ShouldBe(3);
        searchSpace.Contains(offspring).ShouldBeTrue();
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    public void Constructor_RejectsInvalidInternalNodeProbability(double probability)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new SubtreeCrossover(probability));
    }
}
