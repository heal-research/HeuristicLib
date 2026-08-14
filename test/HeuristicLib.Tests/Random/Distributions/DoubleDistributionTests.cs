using HEAL.HeuristicLib.Random.Distributions;
using HEAL.HeuristicLib.Tests.TestSupport.Random;

namespace HEAL.HeuristicLib.Tests.Random.Distributions;

public sealed class DoubleDistributionTests
{
    [Fact]
    public void UniformDoubleDistribution_SamplesFromRange()
    {
        var distribution = new UniformDoubleDistribution(-2.0, 2.0);

        distribution.Sample(new SequenceRandomNumberGenerator(0.75)).ShouldBe(1.0);
        UniformDoubleDistribution.Sample(new SequenceRandomNumberGenerator(0.25), 10.0, 14.0).ShouldBe(11.0);
    }

    [Fact]
    public void UniformDoubleDistribution_CollapsedRangeReturnsTheMinimum()
    {
        UniformDoubleDistribution.Sample(new SequenceRandomNumberGenerator(0.0), minimum: 2.0, maximum: 1.0).ShouldBe(2.0);
        UniformDoubleDistribution.Sample(new SequenceRandomNumberGenerator(0.0), minimum: 2.0, maximum: 2.0).ShouldBe(2.0);
    }

    [Fact]
    public void UniformDoubleDistribution_PropagatesNaNBounds()
    {
        UniformDoubleDistribution.Sample(new SequenceRandomNumberGenerator(0.5), minimum: double.NaN, maximum: 1.0).ShouldBe(double.NaN);
        UniformDoubleDistribution.Sample(new SequenceRandomNumberGenerator(0.5), minimum: 0.0, maximum: double.NaN).ShouldBe(double.NaN);
    }

    [Fact]
    public void NormalDoubleDistribution_NegativeStandardDeviationMirrorsAroundTheMean()
    {
        var positive = NormalDoubleDistribution.Sample(new SequenceRandomNumberGenerator(0.75, 0.5), mean: 0.0, standardDeviation: 1.0);
        var negative = NormalDoubleDistribution.Sample(new SequenceRandomNumberGenerator(0.75, 0.5), mean: 0.0, standardDeviation: -1.0);

        negative.ShouldBe(-positive);
    }

    [Fact]
    public void NormalDoubleDistribution_PropagatesNaNStandardDeviation()
    {
        NormalDoubleDistribution.Sample(new SequenceRandomNumberGenerator(0.75, 0.5), mean: 0.0, standardDeviation: double.NaN)
            .ShouldBe(double.NaN);
    }

    [Fact]
    public void WeightedIndexSampler_RetainsConfiguredWeights()
    {
        var weighted = new WeightedIndexSampler([2.0, 2.0]);
        weighted.Count.ShouldBe(2);
        weighted.Weights.ShouldBe([2.0, 2.0]);

        var uniform = new WeightedIndexSampler(3);
        uniform.Count.ShouldBe(3);
        uniform.Weights.ShouldBeEmpty();
    }

    [Fact]
    public void WeightedIndexSampler_DistinguishesConfigurationsThatSampleUniformly()
    {
        var uniform = new WeightedIndexSampler(2);
        var equalFinite = new WeightedIndexSampler([123.321, 123.321]);
        var allInfinite = new WeightedIndexSampler([double.PositiveInfinity, double.PositiveInfinity]);

        // All three select uniformly, but they remain distinguishable configurations.
        uniform.ShouldNotBe(equalFinite);
        uniform.ShouldNotBe(allInfinite);
        equalFinite.ShouldNotBe(allInfinite);
        new WeightedIndexSampler([123.321, 123.321]).ShouldBe(equalFinite);
        new WeightedIndexSampler(3).ShouldNotBe(uniform);
    }

    [Fact]
    public void WeightedIndexSampler_NeverWeightsAreExcluded()
    {
        // Zero, negative finite values, negative infinity and NaN never participate.
        new WeightedIndexSampler([2.0, 0.0, -1.0, double.NegativeInfinity, double.NaN])
            .Sample(new SequenceRandomNumberGenerator()).ShouldBe(0);
    }

    [Fact]
    public void WeightedIndexSampler_PositiveInfinityOverridesFiniteWeights()
    {
        new WeightedIndexSampler([1.0, double.PositiveInfinity])
            .Sample(new SequenceRandomNumberGenerator()).ShouldBe(1);

        // Several infinite entries share the selection uniformly, ignoring the finite one between them.
        var shared = new WeightedIndexSampler([double.PositiveInfinity, 1.0, double.PositiveInfinity]);
        shared.Sample(new SequenceRandomNumberGenerator(0.0)).ShouldBe(0);
        shared.Sample(new SequenceRandomNumberGenerator(0.99)).ShouldBe(2);
    }

    [Fact]
    public void WeightedIndexSampler_FallsBackToUniformWhenNothingIsSelectable()
    {
        // Every entry means never, so selection spreads uniformly across all of them.
        var unselectable = new WeightedIndexSampler([0.0, -1.0, double.NaN]);

        unselectable.Sample(new SequenceRandomNumberGenerator(0.0)).ShouldBe(0);
        unselectable.Sample(new SequenceRandomNumberGenerator(0.99)).ShouldBe(2);
    }

    [Fact]
    public void WeightedIndexSampler_SamplesPositiveFiniteWeightsProportionally()
    {
        var sampler = new WeightedIndexSampler([1.0, 3.0]);

        sampler.Sample(new SequenceRandomNumberGenerator(0.2)).ShouldBe(0);
        sampler.Sample(new SequenceRandomNumberGenerator(0.9)).ShouldBe(1);
    }

    [Fact]
    public void WeightedIndexSampler_ScalesLargeFiniteWeightsWithoutOverflow()
    {
        var sampler = new WeightedIndexSampler([double.MaxValue, double.MaxValue / 2]);

        sampler.Sample(new SequenceRandomNumberGenerator(0.2)).ShouldBe(0);
        sampler.Sample(new SequenceRandomNumberGenerator(0.8)).ShouldBe(1);
    }

    [Fact]
    public void WeightedIndexSampler_DeterministicOutcomesDoNotConsumeRandomDraws()
    {
        var noDraws = new SequenceRandomNumberGenerator();

        new WeightedIndexSampler(1).Sample(noDraws).ShouldBe(0);
        new WeightedIndexSampler([0.0, double.PositiveInfinity]).Sample(noDraws).ShouldBe(1);
    }

    [Fact]
    public void WeightedIndexSampler_RejectsInvalidDomainsAtConstruction()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new WeightedIndexSampler(0));
        Should.Throw<ArgumentException>(() => new WeightedIndexSampler(Array.Empty<double>()));
        Should.Throw<ArgumentException>(() => new WeightedIndexSampler(3, [1.0, 2.0]));
    }

    [Fact]
    public void WeightedItemSampler_SnapshotsItemsAndWeights()
    {
        var items = new List<string> { "first", "second" };
        var weights = new List<double> { 0.0, 1.0 };
        var sampler = new WeightedItemSampler<string>(items, weights);

        items[1] = "changed";
        weights[1] = 0.0;

        sampler.Items.ShouldBe(["first", "second"]);
        sampler.Weights.ShouldBe([0.0, 1.0]);
        sampler.Sample(new SequenceRandomNumberGenerator()).ShouldBe("second");
    }

    [Fact]
    public void WeightedItemSampler_UsesStructuralConfigurationEquality()
    {
        new WeightedItemSampler<string>(["a", "b"], [1.0, 3.0])
            .ShouldBe(new WeightedItemSampler<string>(["a", "b"], [1.0, 3.0]));
        new WeightedItemSampler<string>(["a", "b"])
            .ShouldNotBe(new WeightedItemSampler<string>(["a", "b"], [1.0, 1.0]));
    }

    [Fact]
    public void WeightedIndexSampler_ReweightingRecompilesTheSamplingRepresentation()
    {
        var original = new WeightedIndexSampler([1.0, double.PositiveInfinity]);

        var reweighted = original with { Weights = [double.PositiveInfinity, 1.0] };

        // The copy must sample from its own weights rather than the representation compiled for the original.
        original.Sample(new SequenceRandomNumberGenerator()).ShouldBe(1);
        reweighted.Sample(new SequenceRandomNumberGenerator()).ShouldBe(0);
        original.Weights.ShouldBe([1.0, double.PositiveInfinity]);
        reweighted.Weights.ShouldBe([double.PositiveInfinity, 1.0]);
        reweighted.Count.ShouldBe(2);
    }

    [Fact]
    public void WeightedIndexSampler_ReweightingToEmptyRestoresUniformSampling()
    {
        var uniform = new WeightedIndexSampler([0.0, 1.0]) with { Weights = [] };

        uniform.Weights.ShouldBeEmpty();
        uniform.Sample(new SequenceRandomNumberGenerator(0.0)).ShouldBe(0);
        uniform.Sample(new SequenceRandomNumberGenerator(0.99)).ShouldBe(1);
        uniform.ShouldBe(new WeightedIndexSampler(2));
    }

    [Fact]
    public void WeightedIndexSampler_ReweightingKeepsTheEntryCountFixed()
    {
        var sampler = new WeightedIndexSampler(count: 3);

        Should.Throw<ArgumentException>(() => { _ = sampler with { Weights = [1.0, 2.0] }; });
    }

    [Fact]
    public void WeightedItemSampler_ReweightingKeepsTheItemsAndRecompiles()
    {
        var original = new WeightedItemSampler<string>(["first", "second"], [1.0, 0.0]);

        var reweighted = original with { Weights = [0.0, 1.0] };

        original.Sample(new SequenceRandomNumberGenerator()).ShouldBe("first");
        reweighted.Sample(new SequenceRandomNumberGenerator()).ShouldBe("second");
        reweighted.Items.ShouldBe(["first", "second"]);
        reweighted.ShouldBe(new WeightedItemSampler<string>(["first", "second"], [0.0, 1.0]));
        reweighted.ShouldNotBe(original);
        Should.Throw<ArgumentException>(() => { _ = original with { Weights = [1.0] }; });
    }

    [Fact]
    public void MixtureDistribution_ReweightingKeepsTheComponents()
    {
        IDistribution<double> low = new UniformDoubleDistribution(0.0, 1.0);
        IDistribution<double> high = new UniformDoubleDistribution(10.0, 11.0);
        var mixture = new MixtureDistribution<double>([low, high], [1.0, 0.0]);

        var reweighted = mixture with { Weights = [0.0, 1.0] };

        mixture.Sample(new SequenceRandomNumberGenerator(0.5, 0.5)).ShouldBe(0.5);
        reweighted.Sample(new SequenceRandomNumberGenerator(0.5, 0.5)).ShouldBe(10.5);
        reweighted.Distributions.ShouldBe([low, high]);
        reweighted.ShouldBe(new MixtureDistribution<double>([low, high], [0.0, 1.0]));
        Should.Throw<ArgumentException>(() => { _ = mixture with { Weights = [1.0, 2.0, 3.0] }; });
    }
}
