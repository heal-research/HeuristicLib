using HEAL.HeuristicLib.Tests.TestSupport.Random;

namespace HEAL.HeuristicLib.Tests.Genotypes.SymbolicExpressions;

public sealed class NumericConfigurationTests
{
    [Fact]
    public void MixtureDistribution_SamplesItsSelectedComponent()
    {
        var distribution = new MixtureDistribution<double>((new UniformDoubleDistribution(-10, 0), 1.0), (new UniformDoubleDistribution(10, 20), 3.0));

        distribution.Sample(new SequenceRandomNumberGenerator(0.9, 0.5)).ShouldBe(15.0);
    }

    [Fact]
    public void MixtureDistribution_CanNestWeightedMixtures()
    {
        var inner = new MixtureDistribution<double>((new UniformDoubleDistribution(0.0, 0.0), 1.0), (new UniformDoubleDistribution(10.0, 10.0), 3.0));
        var outer = new MixtureDistribution<double>((inner, 3.0), (new UniformDoubleDistribution(100.0, 100.0), 1.0));

        outer.Sample(new SequenceRandomNumberGenerator(0.5, 0.9, 0.0)).ShouldBe(10.0);
    }

    [Fact]
    public void MixtureDistribution_SnapshotsReadOnlyListInputsIntoValueArrays()
    {
        var first = new UniformDoubleDistribution(0.0, 1.0);
        var second = new UniformDoubleDistribution(1.0, 2.0);
        var distributions = new List<IDistribution<double>> { first, second };
        var weights = new List<double> { 1.0, 3.0 };

        var mixture = new MixtureDistribution<double>(distributions, weights);
        distributions[0] = second;
        weights[0] = 100.0;

        mixture.Distributions.ShouldBe([first, second]);
        mixture.Weights.ShouldBe([1.0, 3.0]);
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
    public void Choose_SnapshotsReadOnlyListInputsIntoValueArrays()
    {
        var first = new ResampleInitialNumericPerturbation();
        var second = new AdditiveNumericPerturbation(new UniformDoubleDistribution(1.0, 1.0));
        var options = new List<NumericPerturbation> { first, second };
        var weights = new List<double> { 1.0, 3.0 };

        var perturbation = new ChooseNumericPerturbation(options, weights);
        options[0] = second;
        weights[0] = 100.0;

        perturbation.Options.ShouldBe([first, second]);
        perturbation.Weights.ShouldBe([1.0, 3.0]);
    }

    [Fact]
    public void Choose_ReweightingKeepsTheOptions()
    {
        var symbol = new EvolvableConstantSymbol();
        var add = new AdditiveNumericPerturbation(new UniformDoubleDistribution(1.0, 1.0));
        var resample = new ResampleNumericPerturbation(new UniformDoubleDistribution(10.0, 10.0));
        var perturbation = new ChooseNumericPerturbation([add, resample], [1.0, 0.0]);

        var reweighted = perturbation with { Weights = [0.0, 1.0] };

        perturbation.TryApply(2.0, symbol, new SequenceRandomNumberGenerator(0.0), out var added).ShouldBeTrue();
        reweighted.TryApply(2.0, symbol, new SequenceRandomNumberGenerator(0.0), out var resampled).ShouldBeTrue();
        added.ShouldBe(3.0);
        resampled.ShouldBe(10.0);
        reweighted.Options.ShouldBe([add, resample]);
        reweighted.ShouldBe(new ChooseNumericPerturbation([add, resample], [0.0, 1.0]));
        Should.Throw<ArgumentException>(() => { _ = perturbation with { Weights = [1.0] }; });
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
        var firstMixture = new MixtureDistribution<double>((new UniformDoubleDistribution(-1.0, 0.0), 1.0), (new NormalDoubleDistribution(1.0, 2.0), 3.0));
        var secondMixture = new MixtureDistribution<double>((new UniformDoubleDistribution(-1.0, 0.0), 1.0), (new NormalDoubleDistribution(1.0, 2.0), 3.0));
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
    public void EqualWeights_AreRetainedEvenThoughSamplingIsUniform()
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

        variable.SelectionWeights.ShouldBe([2.0, 2.0]);
        mixture.Weights.ShouldBe([2.0, 2.0]);
        perturbation.Weights.ShouldBe([2.0, 2.0]);
    }

    [Fact]
    public void VariableSymbol_RetainsAndUsesItsSelectionWeights()
    {
        var symbol = new VariableSymbol(["x0", "x1"], [1.0, 3.0]);

        var node = symbol.CreateNode(new SequenceRandomNumberGenerator(0.9)).ShouldBeOfType<VariableExpressionNode>();

        node.VariableName.ShouldBe("x1");
        symbol.SelectionWeights.ShouldBe([1.0, 3.0]);
    }

    [Fact]
    public void VariableSymbol_SnapshotsReadOnlyListInputsIntoValueArrays()
    {
        var variables = new List<string> { "x0", "x1" };
        var weights = new List<double> { 1.0, 3.0 };

        var symbol = new VariableSymbol(variables, weights);
        variables[0] = "changed";
        weights[0] = 100.0;

        symbol.Variables.ShouldBe(["x0", "x1"]);
        symbol.SelectionWeights.ShouldBe([1.0, 3.0]);
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
