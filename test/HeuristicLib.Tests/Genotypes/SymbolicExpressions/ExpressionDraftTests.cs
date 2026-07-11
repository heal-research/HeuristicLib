using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;
using HEAL.HeuristicLib.Random.Distributions;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.Genotypes.SymbolicExpressions;

public sealed class ExpressionDraftTests
{
    [Fact]
    public void Build_CreatesTokensWithLocalSymbols()
    {
        var expression = (Variable("x0") + FixedConstant(2.0) * Constant(3.0)).Build();

        expression.ToInfixString().ShouldBe("(x0 + (2 * 3))");
        expression.TraversePostOrder().Select(node => node.Symbol).ShouldBe([
            new VariableSymbol(["x0"]),
            new FixedConstantSymbol(2.0),
            new EvolvableConstantSymbol(),
            new MultiplicationSymbol(),
            new AdditionSymbol()
        ]);
    }

    [Fact]
    public void Build_ResolvesSymbolsFromSearchSpace()
    {
        var variable = new VariableSymbol(["x0", "x1"]);
        var constant = new EvolvableConstantSymbol(new UniformDoubleDistribution(10, 20), new ResampleInitialNumericPerturbation());
        var searchSpace = new ExpressionTreeSearchSpace(10, 5, [new AdditionSymbol(), variable, constant]);

        var expression = (Variable("x0", variable) + Constant(12.0, constant)).Build(searchSpace);

        expression.Root.Symbol.ShouldBe(new AdditionSymbol());
        expression.Root.Child(0).Node.Symbol.ShouldBe(variable);
        expression.Root.Child(1).Node.Symbol.ShouldBe(constant);
    }

    [Fact]
    public void TryBuild_ReturnsFalseForAmbiguousSymbol()
    {
        var searchSpace = new ExpressionTreeSearchSpace(1, 1, [new VariableSymbol(["x0"]), new VariableSymbol(["x0"])]);

        Variable("x0").TryBuild(searchSpace, out _).ShouldBeFalse();
    }

    [Fact]
    public void Build_PreservesRepeatedVariableOccurrences()
    {
        var expression = (Variable("x0") + Variable("x0")).Build();

        expression.TraversePostOrder()
            .Where(node => node.Node.TryGetVariableName(out _))
            .Select(node =>
            {
                node.Node.TryGetVariableName(out var name);
                return name;
            })
            .ShouldBe(["x0", "x0"]);
    }

    [Fact]
    public void Build_PreservesFixedAndEvolvableConstantMetadata()
    {
        var expression = (FixedConstant(1.0) + Constant(2.0)).Build();

        expression.Root.Child(0).Node.Symbol.ShouldBe(new FixedConstantSymbol(1.0));
        expression.Root.Child(1).Node.Symbol.ShouldBe(new EvolvableConstantSymbol());
    }

    [Fact]
    public void ToInfixString_FormatsBinaryExpression()
    {
        (Variable("x0") + FixedConstant(2.0) * Variable("x1")).Build().ToInfixString().ShouldBe("(x0 + (2 * x1))");
    }

    [Fact]
    public void Operators_MapToExpectedOperations()
    {
        (Variable("left") - Variable("right")).Build().ToInfixString().ShouldBe("(left - right)");
        (Variable("left") / Variable("right")).Build().ToInfixString().ShouldBe("(left / right)");
    }

    [Fact]
    public void Apply_RejectsAnIncorrectNumberOfChildren()
    {
        Should.Throw<ArgumentException>(() => ExpressionDraft.Apply(Symbols.Addition, Variable("x0")));
    }
}
