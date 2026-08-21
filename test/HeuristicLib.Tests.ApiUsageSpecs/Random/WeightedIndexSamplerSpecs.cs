using HEAL.HeuristicLib.Encodings.SymbolicExpressions;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Random;

/// <summary>Documents constructing one sampler for repeated draws from a fixed set of entries.</summary>
public class WeightedIndexSamplerSpecs
{
    [Fact]
    public void UniformSamplingFixesTheEntryCountAtConstruction()
    {
        var sampler = new WeightedIndexSampler(count: 3);
        var random = RandomNumberGenerator.Create(1);

        var firstIndex = sampler.Sample(random);
        var secondIndex = sampler.Sample(random);

        firstIndex.ShouldBeInRange(0, sampler.Count - 1);
        secondIndex.ShouldBeInRange(0, sampler.Count - 1);
    }

    [Fact]
    public void WeightedSamplingCompilesTheWeightsAtConstruction()
    {
        IReadOnlyList<double> weights = [1.0, 3.0];
        var sampler = new WeightedIndexSampler(weights);
        IDistribution<int> distribution = sampler;
        var random = RandomNumberGenerator.Create(1);

        var index = distribution.Sample(random);

        index.ShouldBeInRange(0, sampler.Count - 1);
        sampler.Weights.ShouldBe([1.0, 3.0]);
    }

    [Fact]
    public void CountAndOptionalWeightsCanBeSuppliedTogether()
    {
        var uniform = new WeightedIndexSampler(count: 2, weights: null);
        var weighted = new WeightedIndexSampler(count: 2, weights: [1.0, 3.0]);

        uniform.Weights.ShouldBeEmpty();
        weighted.Weights.ShouldBe([1.0, 3.0]);
    }

    [Fact]
    public void WeightedItemSamplerSamplesItemsDirectly()
    {
        var sampler = new WeightedItemSampler<string>(["first", "second"], [0.0, 1.0]);
        IDistribution<string> distribution = sampler;

        distribution.Sample(RandomNumberGenerator.Create(1)).ShouldBe("second");
        sampler.Items.ShouldBe(["first", "second"]);
    }

    /// <summary>Documents retuning a configured sampler without restating the entries it samples from.</summary>
    [Fact]
    public void WeightsAreTheOneMemberAWithExpressionSets()
    {
        var uniform = new WeightedItemSampler<string>(["first", "second", "third"]);

        var favoursTheLast = uniform with { Weights = [1.0, 1.0, 8.0] };
        var backToUniform = favoursTheLast with { Weights = [] };

        // The entries a sampler draws from stay fixed for its lifetime; only their weights are retunable.
        favoursTheLast.Items.ShouldBe(["first", "second", "third"]);
        favoursTheLast.Weights.ShouldBe([1.0, 1.0, 8.0]);
        uniform.Weights.ShouldBeEmpty();
        backToUniform.ShouldBe(uniform);
    }

    /// <summary>Documents retuning the weights a configuration exposes, without rebuilding the configuration.</summary>
    [Fact]
    public void ConfigurationsThatSampleExposeTheSameReweightingShape()
    {
        var variables = new VariableSymbol(["x0", "x1", "x2"]);
        var mixture = new MixtureDistribution<double>([
            new UniformDoubleDistribution(-1.0, 1.0),
            new NormalDoubleDistribution(0.0, 1.0)
        ]);

        var preferredVariables = variables with { SelectionWeights = [3.0, 1.0, 1.0] };
        var preferredComponent = mixture with { Weights = [1.0, 4.0] };

        preferredVariables.Variables.ShouldBe(["x0", "x1", "x2"]);
        preferredVariables.SelectionWeights.ShouldBe([3.0, 1.0, 1.0]);
        preferredComponent.Distributions.Count.ShouldBe(2);
        preferredComponent.Weights.ShouldBe([1.0, 4.0]);

        // Sampling a different set of variables means constructing a new symbol rather than reusing this one.
        preferredVariables.ShouldNotBe(variables);
    }
}
