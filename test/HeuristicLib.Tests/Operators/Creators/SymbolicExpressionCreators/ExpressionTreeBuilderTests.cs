using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Operators.Creators.SymbolicExpressionCreators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.Operators.Creators.SymbolicExpressionCreators;

public sealed class ExpressionTreeBuilderTests
{
    [Fact]
    public void Build_MaterializesACompletedConstruction()
    {
        var random = RandomNumberGenerator.Create(123);
        var variable = Symbols.Variable(["x0"]);
        var builder = new ExpressionTreeBuilder();
        var children = builder.Expand(builder.Root, Symbols.Addition);
        builder.CompleteWithTerminal(children[0], variable.CreateNode(random));
        builder.CompleteWithTerminal(children[1], variable.CreateNode(random));

        var root = builder.Build(random);
        var expression = new ExpressionTree(root);

        expression.ToInfixString().ShouldBe("(x0 + x0)");
        expression.Length.ShouldBe(3);
    }

    [Fact]
    public void Build_RejectsAnUnfilledPosition()
    {
        var builder = new ExpressionTreeBuilder();
        builder.Expand(builder.Root, Symbols.Addition);

        Should.Throw<InvalidOperationException>(() =>
            builder.Build(RandomNumberGenerator.Create(123)));
    }

    [Fact]
    public void PositionsCannotBeCompletedOrExpandedMoreThanOnce()
    {
        var random = RandomNumberGenerator.Create(123);
        var variable = Symbols.Variable(["x0"]);
        var builder = new ExpressionTreeBuilder();
        builder.CompleteWithTerminal(builder.Root, variable.CreateNode(random));

        Should.Throw<InvalidOperationException>(() =>
            builder.CompleteWithTerminal(builder.Root, variable.CreateNode(random)));
        Should.Throw<InvalidOperationException>(() =>
            builder.Expand(builder.Root, Symbols.Negation));
    }

    [Fact]
    public void PositionsCannotBeUsedWithAnotherBuilder()
    {
        var first = new ExpressionTreeBuilder();
        var second = new ExpressionTreeBuilder();

        Should.Throw<ArgumentException>(() =>
            second.Expand(first.Root, Symbols.Negation));
    }
}
