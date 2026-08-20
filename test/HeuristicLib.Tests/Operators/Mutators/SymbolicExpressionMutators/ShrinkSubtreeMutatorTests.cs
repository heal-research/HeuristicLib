using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionMutators;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;
using HEAL.HeuristicLib.Tests.TestSupport.Random;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.Operators.Mutators.SymbolicExpressionMutators;

public sealed class ShrinkSubtreeMutatorTests
{
    [Fact]
    public void Mutate_ReplacesTheSelectedOperationWithAnAllowedTerminal()
    {
        var searchSpace = CreateSearchSpace();
        var parent = (Variable("x0") + Variable("x1") * Variable("x2")).Build(searchSpace);

        var offspring = ShrinkSubtreeMutation.Mutate(
            parent,
            new SequenceRandomNumberGenerator(
                0.0,  // Select the root from the two operation points.
                0.75), // Sample x2 from the variable symbol.
            searchSpace);

        offspring.ToInfixString().ShouldBe("x2");
        offspring.Length.ShouldBe(1);
        searchSpace.Contains(offspring).ShouldBeTrue();
        parent.ToInfixString().ShouldBe("(x0 + (x1 * x2))");
    }

    [Fact]
    public void Mutate_RebuildsOnlyTheAncestorsOfANestedSelection()
    {
        var searchSpace = CreateSearchSpace();
        var parent = (Variable("x0") + Variable("x1") * Variable("x2")).Build(searchSpace);
        var parentRoot = parent.Root.ShouldBeOfType<BinaryExpressionNode>();

        var offspring = ShrinkSubtreeMutation.Mutate(
            parent,
            new SequenceRandomNumberGenerator(
                0.75, // Select the multiplication from the two operation points.
                0.75), // Sample x2 from the variable symbol.
            searchSpace);

        var offspringRoot = offspring.Root.ShouldBeOfType<BinaryExpressionNode>();
        offspring.ToInfixString().ShouldBe("(x0 + x2)");
        offspringRoot.Left.ShouldBeSameAs(parentRoot.Left);
        offspring.Length.ShouldBe(3);
        searchSpace.Contains(offspring).ShouldBeTrue();
        parent.ToInfixString().ShouldBe("(x0 + (x1 * x2))");
    }

    [Fact]
    public void Mutate_ReturnsTheOriginalTerminalTreeWithoutUsingRandomness()
    {
        var searchSpace = CreateSearchSpace();
        var parent = Variable("x0").Build(searchSpace);

        var offspring = ShrinkSubtreeMutation.Mutate(
            parent,
            new SequenceRandomNumberGenerator(),
            searchSpace);

        offspring.ShouldBeSameAs(parent);
    }

    [Fact]
    public void Mutate_UsesTheOperatorEntryPointAndPreservesContainment()
    {
        var searchSpace = CreateSearchSpace();
        var parent = (Variable("x0") + Variable("x1") * Variable("x2")).Build(searchSpace);

        var offspring = new ShrinkSubtreeMutator().MutateCandidate(
            parent,
            new SequenceRandomNumberGenerator(0.75, 0.75),
            searchSpace);

        offspring.ToInfixString().ShouldBe("(x0 + x2)");
        searchSpace.Contains(offspring).ShouldBeTrue();
    }

    private static ExpressionTreeSearchSpace CreateSearchSpace() =>
        new(
            maximumLength: 5,
            maximumDepth: 3,
            operations: [Symbols.Addition, Symbols.Multiplication],
            variables: ["x0", "x1", "x2"],
            constants: []);
}
