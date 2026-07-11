using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionMutators;
using HEAL.HeuristicLib.Random.Distributions;
using HEAL.HeuristicLib.Tests.TestSupport.Random;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

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

        result.Root.Node.Symbol.ShouldBe(narrow);
        result.Root.Node.NumericValue.ShouldBe(0.5);
        result.Root.Node.ShouldNotBe(new ExpressionNode(broad, 0.5));
    }

    [Fact]
    public void Mutate_AllowsVariableNoOpsAndTargetsEveryEligibleTokenOnce()
    {
        var variables = new VariableSymbol(["x0"]);
        var expression = (Variable("x0", variables) + Variable("x0", variables)).Build();

        var result = LocalPerturbationMutation.Mutate(expression, new SequenceRandomNumberGenerator(0.0, 0.0), new AllLocalPerturbationTargets());

        result.ShouldBe(expression);
    }
}
