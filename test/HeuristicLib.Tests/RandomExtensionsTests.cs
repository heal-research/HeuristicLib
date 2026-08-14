using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Tests.TestSupport.Random;

namespace HEAL.HeuristicLib.Tests;

public sealed class RandomExtensionsTests
{
    [Theory]
    [InlineData(-0.1, false)]
    [InlineData(double.NegativeInfinity, false)]
    [InlineData(double.NaN, false)]
    [InlineData(1.1, true)]
    [InlineData(double.PositiveInfinity, true)]
    public void NextBool_UnusualProbabilitiesUseThresholdSemantics(double probability, bool expected)
    {
        var result = new SequenceRandomNumberGenerator(0.5).NextBool(probability);

        result.ShouldBe(expected);
    }

    [Fact]
    public void NextBools_UnusualProbabilitiesUseThresholdSemantics()
    {
        new SequenceRandomNumberGenerator(0.1, 0.9).NextBools(2, double.NaN).ShouldBe([false, false]);
        new SequenceRandomNumberGenerator(0.1, 0.9).NextBools(2, double.PositiveInfinity).ShouldBe([true, true]);
    }

    [Fact]
    public void NextNormal_NegativeSigmaMirrorsTheSampleAroundTheMean()
    {
        const double mean = 3;
        var positive = new SequenceRandomNumberGenerator(0.75, 0.5).NextNormal(mean, sigma: 2);
        var negative = new SequenceRandomNumberGenerator(0.75, 0.5).NextNormal(mean, sigma: -2);

        negative.ShouldBe((2 * mean) - positive, tolerance: 1e-12);
    }

    [Fact]
    public void NextNormals_NegativeSigmaIsAcceptedForEmptyOutput()
    {
        var values = new SequenceRandomNumberGenerator().NextNormals(0, sigma: -1);

        values.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(4, 4)]
    [InlineData(7, 3)]
    public void NextInt_CollapsedRangeReturnsLowWithoutDrawing(int low, int high)
    {
        new SequenceRandomNumberGenerator().NextInt(low, high).ShouldBe(low);
    }

    [Theory]
    [InlineData(4.0, 4.0)]
    [InlineData(7.0, 3.0)]
    public void NextDouble_CollapsedRangeReturnsLowWithoutDrawing(double low, double high)
    {
        new SequenceRandomNumberGenerator().NextDouble(low, high).ShouldBe(low);
    }

    [Fact]
    public void NextInts_CollapsedRangeReturnsLowWithoutDrawing()
    {
        new SequenceRandomNumberGenerator().NextInts(3, 7, 3).ShouldBe([7, 7, 7]);
    }

    [Fact]
    public void NextDoubles_CollapsedRangeReturnsLowWithoutDrawing()
    {
        new SequenceRandomNumberGenerator().NextDoubles(3, 7, 3).ShouldBe([7, 7, 7]);
    }

    [Fact]
    public void NextBools_FillsDestinationSpan()
    {
        var destination = new bool[2];

        new SequenceRandomNumberGenerator(0.1, 0.9).NextBools(destination, probability: 0.5);

        destination.ShouldBe([true, false]);
    }

    [Fact]
    public void NextNormals_FillsDestinationSpan()
    {
        var destination = new double[2];

        new SequenceRandomNumberGenerator(0.75, 0.5, 0.75, 0.5).NextNormals(destination, mu: 3, sigma: 0);

        destination.ShouldBe([3, 3]);
    }

    [Fact]
    public void NextDoubles_FillsDestinationSpan()
    {
        var destination = new double[2];

        new SequenceRandomNumberGenerator(0.25, 0.75).NextDoubles(destination, low: 10, high: 20);

        destination.ShouldBe([12.5, 17.5]);
    }

    [Fact]
    public void NextInts_FillsDestinationSpan()
    {
        var destination = new int[2];

        new SequenceRandomNumberGenerator(0.25, 0.75).NextInts(destination, low: 10, high: 20);

        destination.ShouldBe([12, 17]);
    }
}
