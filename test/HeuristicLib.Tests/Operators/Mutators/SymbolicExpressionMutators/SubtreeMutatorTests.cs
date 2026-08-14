using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionMutators;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;
using HEAL.HeuristicLib.Tests.TestSupport.Random;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.Operators.Mutators.SymbolicExpressionMutators;

public sealed class SubtreeMutatorTests
{
    [Fact]
    public void Mutate_PreservesAggregateContainmentForANonCanonicalVariableOrigin()
    {
        var origin = new VariableSymbol(["x0", "x1"]);
        var parent = new ExpressionTree(new VariableExpressionNode(origin, "x0"));
        var searchSpace = new ExpressionTreeSearchSpace(1, 1,
        [
            new VariableSymbol(["x0"]),
            new VariableSymbol(["x1"])
        ]);

        var offspring = SubtreeMutation.Mutate(
            parent,
            new SequenceRandomNumberGenerator(0.0, 0.9),
            searchSpace);

        searchSpace.Contains(parent).ShouldBeTrue();
        offspring.ToInfixString().ShouldBe("x1");
        searchSpace.Contains(offspring).ShouldBeTrue();
    }

    [Fact]
    public void Mutate_ReplacesTheSelectedSubtreeWithinItsRemainingBudgets()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 7,
            maximumDepth: 4,
            operations: [Symbols.Addition, Symbols.Multiplication],
            variables: ["x0", "x1"],
            constants: []);
        var parent = (Variable("x0") + Variable("x1") * Variable("x0")).Build(searchSpace);
        var parentRoot = parent.Root.ShouldBeOfType<BinaryExpressionNode>();
        var random = new SequenceRandomNumberGenerator(
            0.5,  // Select the multiplication subtree at preorder index 2.
            0.0,  // Create an addition as the replacement root.
            0.0,  // Allocate one node to the first child.
            0.0,  // Select the variable terminal symbol.
            0.75, // Sample x1.
            0.0,  // Allocate one node to the second child.
            0.0,  // Select the variable terminal symbol.
            0.0); // Sample x0.

        var offspring = SubtreeMutation.Mutate(parent, random, searchSpace);

        offspring.ToInfixString().ShouldBe("(x0 + (x1 + x0))");
        offspring.Length.ShouldBe(5);
        offspring.Depth.ShouldBe(3);
        searchSpace.Contains(offspring).ShouldBeTrue();
        parent.ToInfixString().ShouldBe("(x0 + (x1 * x0))");
        offspring.Root.ShouldBeOfType<BinaryExpressionNode>().Left.ShouldBeSameAs(parentRoot.Left);
    }

    [Fact]
    public void Mutate_UsesATerminalWhenOnlyOneNodeRemainsAvailable()
    {
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 5,
            maximumDepth: 3,
            operations: [Symbols.Addition, Symbols.Multiplication],
            variables: ["x0", "x1"],
            constants: []);
        var parent = (Variable("x0") + Variable("x1") * Variable("x0")).Build(searchSpace);
        var random = new SequenceRandomNumberGenerator(
            0.3,  // Select the left terminal at preorder index 1.
            0.0,  // Select the variable terminal symbol.
            0.75); // Sample x1.

        var offspring = new SubtreeMutator().MutateCandidate(parent, random, searchSpace);

        offspring.ToInfixString().ShouldBe("(x1 + (x1 * x0))");
        offspring.Length.ShouldBe(5);
        offspring.Depth.ShouldBe(3);
        searchSpace.Contains(offspring).ShouldBeTrue();
    }
}
