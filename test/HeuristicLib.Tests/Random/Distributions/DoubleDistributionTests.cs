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
    public void UniformDoubleDistribution_RejectsInvalidRange()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
          UniformDoubleDistribution.Sample(new SequenceRandomNumberGenerator(0.0), minimum: 2.0, maximum: 1.0));
    }

    [Fact]
    public void NormalDoubleDistribution_RejectsNegativeStandardDeviation()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
          NormalDoubleDistribution.Sample(new SequenceRandomNumberGenerator(0.5, 0.5), mean: 0.0, standardDeviation: -1.0));
    }
}
