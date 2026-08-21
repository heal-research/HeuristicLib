using HEAL.HeuristicLib.Tests.TestSupport.Random;
using static HEAL.HeuristicLib.Encodings.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.Operators.Mutators.SymbolicExpressionMutators;

public sealed class LocalPerturbationMutatorTests
{
    [Fact]
    public void Mutate_UsesTheOriginatingEvolvableConstantSymbol()
    {
        var narrow = new EvolvableConstantSymbol(new UniformDoubleDistribution(-1, 1), new ResampleInitialNumericPerturbation());
        var broad = new EvolvableConstantSymbol(new UniformDoubleDistribution(100, 200), new ResampleInitialNumericPerturbation());
        var parent = Constant(0.0, narrow).Build();

        var result = LocalPerturbationMutation.Mutate(parent, new SequenceRandomNumberGenerator(0.0, 0.75), new OneLocalPerturbationTarget());

        var constant = result.Root.ShouldBeOfType<NumericConstantExpressionNode>();
        constant.Symbol.ShouldBe(narrow);
        constant.Value.ShouldBe(0.5);
        constant.ShouldNotBe(new NumericConstantExpressionNode(broad, 0.5));
    }

    [Fact]
    public void Mutate_AllowsVariableNoOpsAndTargetsEveryEligibleNodeOnce()
    {
        var variables = new VariableSymbol(["x0"]);
        var expression = (Variable("x0", variables) + Variable("x0", variables)).Build();

        var result = LocalPerturbationMutation.Mutate(expression, new SequenceRandomNumberGenerator(0.0, 0.0), new AllLocalPerturbationTargets());

        result.ShouldBe(expression);
    }

    [Fact]
    public void Mutate_PreservesAggregateContainmentForAnOriginSpanningSeparateVariableSymbols()
    {
        var origin = new VariableSymbol(["x0", "x1"]);
        var parent = new ExpressionTree(new VariableExpressionNode(origin, "x0"));
        var searchSpace = new ExpressionTreeSearchSpace(1, 1,
        [
            new VariableSymbol(["x0"]),
            new VariableSymbol(["x1"])
        ]);

        var offspring = new LocalPerturbationMutator().MutateCandidate(
            parent,
            new SequenceRandomNumberGenerator(0.0, 0.9),
            searchSpace);

        searchSpace.Contains(parent).ShouldBeTrue();
        offspring.ToInfixString().ShouldBe("x1");
        searchSpace.Contains(offspring).ShouldBeTrue();
    }

    [Fact]
    public void Mutate_AllRebuildsAffectedPathsAndSharesUnaffectedBranches()
    {
        var constant = new EvolvableConstantSymbol(
            new UniformDoubleDistribution(-1.0, 1.0),
            new AdditiveNumericPerturbation(new UniformDoubleDistribution(1.0, 1.0)));
        var parent = (Variable("x0") + Constant(1.0, constant) * Constant(2.0, constant)).Build();

        var result = LocalPerturbationMutation.Mutate(
            parent,
            new SequenceRandomNumberGenerator(0.0, 0.0),
            LocalPerturbationTargets.All);

        result.ToInfixString().ShouldBe("(x0 + (2 * 3))");
        result.Length.ShouldBe(parent.Length);
        result.Depth.ShouldBe(parent.Depth);
        var resultRoot = result.Root.ShouldBeOfType<BinaryExpressionNode>();
        var parentRoot = parent.Root.ShouldBeOfType<BinaryExpressionNode>();
        resultRoot.Left.ShouldBeSameAs(parentRoot.Left);
        parent.ToInfixString().ShouldBe("(x0 + (1 * 2))");
    }

    [Fact]
    public void Mutate_EachWithZeroProbabilityReturnsTheOriginalTree()
    {
        var constant = new EvolvableConstantSymbol(
            new UniformDoubleDistribution(-1.0, 1.0),
            new AdditiveNumericPerturbation(new UniformDoubleDistribution(1.0, 1.0)));
        var parent = (Constant(1.0, constant) + Constant(2.0, constant)).Build();

        var result = LocalPerturbationMutation.Mutate(
            parent,
            new SequenceRandomNumberGenerator(0.0, 0.0),
            LocalPerturbationTargets.Each(0.0));

        result.ShouldBeSameAs(parent);
    }

    [Fact]
    public void Mutate_ReturnsTheOriginalTreeWhenNoNodeSupportsLocalPerturbation()
    {
        var parent = FixedConstant(1.0).Build();

        var result = LocalPerturbationMutation.Mutate(
            parent,
            new SequenceRandomNumberGenerator(),
            LocalPerturbationTargets.One);

        result.ShouldBeSameAs(parent);
    }

    [Fact]
    public void Mutate_EachWithOneProbabilityPerturbsEveryEligibleNode()
    {
        var constant = new EvolvableConstantSymbol(
            new UniformDoubleDistribution(-1.0, 1.0),
            new AdditiveNumericPerturbation(new UniformDoubleDistribution(1.0, 1.0)));
        var parent = (Constant(1.0, constant) + Constant(2.0, constant)).Build();

        var result = LocalPerturbationMutation.Mutate(
            parent,
            new SequenceRandomNumberGenerator(0.0, 0.0, 0.0, 0.0),
            LocalPerturbationTargets.Each(1.0));

        result.ToInfixString().ShouldBe("(2 + 3)");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void Each_RetainsProbabilitiesOutsideTheUnitRange(double probability)
    {
        LocalPerturbationTargets.Each(probability)
            .ShouldBeOfType<EachLocalPerturbationTarget>()
            .Probability.ShouldBe(probability);
    }
}
