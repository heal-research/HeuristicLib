using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Random.Distributions;
using HEAL.HeuristicLib.Tests.TestSupport.Random;

namespace HEAL.HeuristicLib.Tests.Genotypes.SymbolicExpressions;

public sealed class NumericConfigurationTests
{
    [Fact]
    public void MixtureDistribution_SamplesItsSelectedComponent()
    {
        var distribution = new MixtureDistribution<double>([
            (new UniformDoubleDistribution(-10, 0), 1.0),
            (new UniformDoubleDistribution(10, 20), 3.0)
        ]);

        distribution.Sample(new SequenceRandomNumberGenerator(0.9, 0.5)).ShouldBe(15.0);
    }

    [Fact]
    public void Chain_AppliesPerturbationsInOrder()
    {
        var symbol = new EvolvableConstantSymbol();
        var perturbation = new ChainNumericPerturbation([
            new AdditiveNumericPerturbation(new UniformDoubleDistribution(2, 2)),
            new MultiplicativeNumericPerturbation(new UniformDoubleDistribution(0.5, 0.5))
        ]);

        perturbation.TryApply(2.0, symbol, new SequenceRandomNumberGenerator(0.0, 0.0), out var value).ShouldBeTrue();
        value.ShouldBe(6.0);
    }
}
