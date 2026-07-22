using System.Collections.Immutable;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Random;
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
    public void MixtureDistribution_CanNestWeightedMixtures()
    {
        var inner = new MixtureDistribution<double>([
            (new UniformDoubleDistribution(0.0, 0.0), 1.0),
            (new UniformDoubleDistribution(10.0, 10.0), 3.0)
        ]);
        var outer = new MixtureDistribution<double>([
            (inner, 3.0),
            (new UniformDoubleDistribution(100.0, 100.0), 1.0)
        ]);

        outer.Sample(new SequenceRandomNumberGenerator(0.5, 0.9, 0.0)).ShouldBe(10.0);
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

    [Fact]
    public void Choose_SelectsAndAppliesTheWeightedPerturbation()
    {
        var symbol = new EvolvableConstantSymbol();
        var perturbation = new ChooseNumericPerturbation([
            (new AdditiveNumericPerturbation(new UniformDoubleDistribution(1.0, 1.0)), 1.0),
            (new ResampleNumericPerturbation(new UniformDoubleDistribution(10.0, 10.0)), 3.0)
        ]);

        perturbation.TryApply(2.0, symbol, new SequenceRandomNumberGenerator(0.9, 0.0), out var value).ShouldBeTrue();

        value.ShouldBe(10.0);
    }

    [Fact]
    public void DefaultPerturbation_UsesLocalAndResamplingBranches()
    {
        var symbol = new EvolvableConstantSymbol();

        NumericPerturbation.Default.TryApply(2.0, symbol, new SequenceRandomNumberGenerator(0.1, 0.75), out var local).ShouldBeTrue();
        NumericPerturbation.Default.TryApply(2.0, symbol, new SequenceRandomNumberGenerator(0.9, 0.75), out var resampled).ShouldBeTrue();

        local.ShouldBe(2.1, tolerance: 1e-12);
        resampled.ShouldBe(0.5, tolerance: 1e-12);
    }

    [Fact]
    public void Chain_RollsBackWhenAStageIsInapplicable()
    {
        var symbol = new EvolvableConstantSymbol();
        var perturbation = new ChainNumericPerturbation([
            new AdditiveNumericPerturbation(new UniformDoubleDistribution(1.0, 1.0)),
            new InapplicableNumericPerturbation()
        ]);

        perturbation.TryApply(2.0, symbol, new SequenceRandomNumberGenerator(0.0), out var value).ShouldBeFalse();

        value.ShouldBe(2.0);
    }

    [Fact]
    public void Perturbation_AllowsSuccessfulNoOps()
    {
        var symbol = new EvolvableConstantSymbol();
        var perturbation = new AdditiveNumericPerturbation(new UniformDoubleDistribution(0.0, 0.0));

        perturbation.TryApply(2.0, symbol, new SequenceRandomNumberGenerator(0.0), out var value).ShouldBeTrue();

        value.ShouldBe(2.0);
    }

    [Fact]
    public void CompositeConfigurations_HaveStructuralValueEquality()
    {
        var firstMixture = new MixtureDistribution<double>([
            (new UniformDoubleDistribution(-1.0, 0.0), 1.0),
            (new NormalDoubleDistribution(1.0, 2.0), 3.0)
        ]);
        var secondMixture = new MixtureDistribution<double>([
            (new UniformDoubleDistribution(-1.0, 0.0), 1.0),
            (new NormalDoubleDistribution(1.0, 2.0), 3.0)
        ]);
        var firstPerturbation = new ChooseNumericPerturbation([
            (new ResampleNumericPerturbation(firstMixture), 1.0),
            (new ResampleInitialNumericPerturbation(), 1.0)
        ]);
        var secondPerturbation = new ChooseNumericPerturbation([
            (new ResampleNumericPerturbation(secondMixture), 1.0),
            (new ResampleInitialNumericPerturbation(), 1.0)
        ]);
        var firstChain = new ChainNumericPerturbation([firstPerturbation]);
        var secondChain = new ChainNumericPerturbation([secondPerturbation]);

        firstMixture.ShouldBe(secondMixture);
        firstPerturbation.ShouldBe(secondPerturbation);
        firstPerturbation.GetHashCode().ShouldBe(secondPerturbation.GetHashCode());
        firstChain.ShouldBe(secondChain);
    }

    [Fact]
    public void EqualWeights_UseTheUniformSelectionFastPath()
    {
        var variable = new VariableSymbol(["x0", "x1"], [2.0, 2.0]);
        var mixture = new MixtureDistribution<double>(
            ImmutableArray.Create<IDistribution<double>>(
                new UniformDoubleDistribution(0.0, 1.0),
                new UniformDoubleDistribution(1.0, 2.0)),
            [2.0, 2.0]);
        var perturbation = new ChooseNumericPerturbation(
            [new ResampleInitialNumericPerturbation(), new ResampleInitialNumericPerturbation()],
            [2.0, 2.0]);

        variable.SelectionWeights.ShouldBeEmpty();
        mixture.Weights.ShouldBeEmpty();
        perturbation.Weights.ShouldBeEmpty();
    }

    [Fact]
    public void VariableSymbol_UsesItsNormalizedSelectionWeights()
    {
        var symbol = new VariableSymbol(["x0", "x1"], [1.0, 3.0]);

        var node = symbol.CreateNode(new SequenceRandomNumberGenerator(0.9)).ShouldBeOfType<VariableExpressionNode>();

        node.VariableName.ShouldBe("x1");
        symbol.SelectionWeights.ShouldBe([0.25, 0.75]);
    }

    private sealed record InapplicableNumericPerturbation : NumericPerturbation
    {
        public override bool TryApply(double value, EvolvableConstantSymbol symbol, IRandomNumberGenerator random, out double perturbed)
        {
            perturbed = value + 100.0;
            return false;
        }
    }
}
