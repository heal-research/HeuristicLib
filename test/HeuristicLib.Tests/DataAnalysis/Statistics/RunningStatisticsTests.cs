using HEAL.HeuristicLib.DataAnalysis.Statistics;

namespace HEAL.HeuristicLib.Tests.DataAnalysis.Statistics;

public sealed class RunningStatisticsTests
{
    private static readonly double[] Values = [2.0, 4.0, 4.0, 4.0, 5.0, 5.0, 7.0, 9.0];

    [Fact]
    public void RunningMean_AccumulatesRangesAndResets()
    {
        var statistics = new RunningMean();

        statistics.Mean.ShouldBe(double.NaN);
        statistics.AddRange(Values);
        statistics.Count.ShouldBe(8);
        statistics.Mean.ShouldBe(5.0, tolerance: 1e-12);

        statistics.Reset();
        statistics.Count.ShouldBe(0);
        statistics.Mean.ShouldBe(double.NaN);
    }

    [Fact]
    public void RunningMean_MergesIndependentPartitions()
    {
        var first = new RunningMean();
        first.AddRange(Values.AsSpan(0, 3));
        var second = new RunningMean();
        second.AddRange(Values.AsSpan(3));

        first.Merge(second);

        first.Count.ShouldBe(Values.Length);
        first.Mean.ShouldBe(5.0, tolerance: 1e-12);
    }

    [Fact]
    public void RunningMeanVariance_CalculatesKnownStatistics()
    {
        var statistics = new RunningMeanVariance();
        statistics.AddRange(Values);

        statistics.Count.ShouldBe(8);
        statistics.Mean.ShouldBe(5.0, tolerance: 1e-12);
        statistics.PopulationVariance.ShouldBe(4.0, tolerance: 1e-12);
        statistics.SampleVariance.ShouldBe(32.0 / 7.0, tolerance: 1e-12);
        statistics.PopulationStandardDeviation.ShouldBe(2.0, tolerance: 1e-12);
        statistics.SampleStandardDeviation.ShouldBe(Math.Sqrt(32.0 / 7.0), tolerance: 1e-12);
    }

    [Fact]
    public void RunningMeanVariance_MergeMatchesScalarAccumulation()
    {
        var expected = new RunningMeanVariance();
        foreach (var value in Values)
            expected.Add(value);

        var actual = new RunningMeanVariance();
        actual.AddRange(Values.AsSpan(0, 5));
        var remainder = new RunningMeanVariance();
        remainder.AddRange(Values.AsSpan(5));
        actual.Merge(remainder);

        actual.Count.ShouldBe(expected.Count);
        actual.Mean.ShouldBe(expected.Mean, tolerance: 1e-12);
        actual.PopulationVariance.ShouldBe(expected.PopulationVariance, tolerance: 1e-12);
        actual.SampleVariance.ShouldBe(expected.SampleVariance, tolerance: 1e-12);
    }

    [Fact]
    public void RunningMeanVariance_RemainsStableForValuesWithALargeOffset()
    {
        double[] values = [1e12 + 1.0, 1e12 + 2.0, 1e12 + 3.0];
        var statistics = new RunningMeanVariance();

        statistics.AddRange(values);

        statistics.Mean.ShouldBe(1e12 + 2.0, tolerance: 1e-6);
        statistics.PopulationVariance.ShouldBe(2.0 / 3.0, tolerance: 1e-12);
    }

    [Fact]
    public void RunningMoments_CalculatesKnownStatistics()
    {
        var statistics = new RunningMoments();
        statistics.AddRange(Values);

        statistics.Count.ShouldBe(8);
        statistics.Mean.ShouldBe(5.0, tolerance: 1e-12);
        statistics.PopulationVariance.ShouldBe(4.0, tolerance: 1e-12);
        statistics.SampleVariance.ShouldBe(32.0 / 7.0, tolerance: 1e-12);
        statistics.PopulationSkewness.ShouldBe(0.65625, tolerance: 1e-12);
        statistics.SampleSkewness.ShouldBe(Math.Sqrt(56.0) / 6.0 * 0.65625, tolerance: 1e-12);
        statistics.PopulationExcessKurtosis.ShouldBe(-0.21875, tolerance: 1e-12);
        statistics.SampleExcessKurtosis.ShouldBe(0.940625, tolerance: 1e-12);
    }

    [Fact]
    public void RunningMoments_MergeMatchesScalarAccumulation()
    {
        var expected = new RunningMoments();
        foreach (var value in Values)
            expected.Add(value);

        var actual = new RunningMoments();
        actual.AddRange(Values.AsSpan(0, 2));
        var middle = new RunningMoments();
        middle.AddRange(Values.AsSpan(2, 3));
        var remainder = new RunningMoments();
        remainder.AddRange(Values.AsSpan(5));
        actual.Merge(middle);
        actual.Merge(remainder);

        actual.Count.ShouldBe(expected.Count);
        actual.Mean.ShouldBe(expected.Mean, tolerance: 1e-12);
        actual.PopulationVariance.ShouldBe(expected.PopulationVariance, tolerance: 1e-12);
        actual.PopulationSkewness.ShouldBe(expected.PopulationSkewness, tolerance: 1e-12);
        actual.PopulationExcessKurtosis.ShouldBe(expected.PopulationExcessKurtosis, tolerance: 1e-12);
    }

    [Fact]
    public void RunningCovariance_CalculatesKnownStatistics()
    {
        double[] x = [1.0, 2.0, 3.0];
        double[] y = [2.0, 4.0, 5.0];
        var statistics = new RunningCovariance();

        statistics.AddRange(x, y);

        statistics.Count.ShouldBe(3);
        statistics.MeanX.ShouldBe(2.0, tolerance: 1e-12);
        statistics.MeanY.ShouldBe(11.0 / 3.0, tolerance: 1e-12);
        statistics.PopulationVarianceX.ShouldBe(2.0 / 3.0, tolerance: 1e-12);
        statistics.PopulationVarianceY.ShouldBe(14.0 / 9.0, tolerance: 1e-12);
        statistics.PopulationCovariance.ShouldBe(1.0, tolerance: 1e-12);
        statistics.SampleCovariance.ShouldBe(1.5, tolerance: 1e-12);
        statistics.Correlation.ShouldBe(3.0 / Math.Sqrt(28.0 / 3.0), tolerance: 1e-12);
    }

    [Fact]
    public void RunningCovariance_MergeMatchesScalarAccumulation()
    {
        double[] x = [1.0, 2.0, 3.0, 5.0, 8.0];
        double[] y = [2.0, 4.0, 5.0, 7.0, 11.0];
        var expected = new RunningCovariance();
        for (var i = 0; i < x.Length; i++)
            expected.Add(x[i], y[i]);

        var actual = new RunningCovariance();
        actual.AddRange(x.AsSpan(0, 2), y.AsSpan(0, 2));
        var remainder = new RunningCovariance();
        remainder.AddRange(x.AsSpan(2), y.AsSpan(2));
        actual.Merge(remainder);

        actual.Count.ShouldBe(expected.Count);
        actual.MeanX.ShouldBe(expected.MeanX, tolerance: 1e-12);
        actual.MeanY.ShouldBe(expected.MeanY, tolerance: 1e-12);
        actual.PopulationVarianceX.ShouldBe(expected.PopulationVarianceX, tolerance: 1e-12);
        actual.PopulationVarianceY.ShouldBe(expected.PopulationVarianceY, tolerance: 1e-12);
        actual.PopulationCovariance.ShouldBe(expected.PopulationCovariance, tolerance: 1e-12);
        actual.Correlation.ShouldBe(expected.Correlation, tolerance: 1e-12);
    }

    [Fact]
    public void RunningCovariance_RejectsMismatchedRanges()
    {
        var statistics = new RunningCovariance();

        Should.Throw<ArgumentException>(() => statistics.AddRange([1.0], [1.0, 2.0]));
    }

    [Fact]
    public void BatchUpdatesMatchScalarUpdatesForLargeInputs()
    {
        var values = Enumerable.Range(0, 1024)
            .Select(index => 1e9 + Math.Sin(index * 0.17) * 10.0 + index % 7)
            .ToArray();
        var pairedValues = values
            .Select((value, index) => value * 1.25 + Math.Cos(index * 0.11))
            .ToArray();

        var scalarMean = new RunningMean();
        var scalarVariance = new RunningMeanVariance();
        var scalarMoments = new RunningMoments();
        var scalarCovariance = new RunningCovariance();
        for (var i = 0; i < values.Length; i++)
        {
            scalarMean.Add(values[i]);
            scalarVariance.Add(values[i]);
            scalarMoments.Add(values[i]);
            scalarCovariance.Add(values[i], pairedValues[i]);
        }

        var batchMean = new RunningMean();
        batchMean.AddRange(values);
        var batchVariance = new RunningMeanVariance();
        batchVariance.AddRange(values);
        var batchMoments = new RunningMoments();
        batchMoments.AddRange(values);
        var batchCovariance = new RunningCovariance();
        batchCovariance.AddRange(values, pairedValues);

        batchMean.Mean.ShouldBe(scalarMean.Mean, tolerance: 1e-5);
        batchVariance.PopulationVariance.ShouldBe(scalarVariance.PopulationVariance, tolerance: 1e-6);
        batchMoments.PopulationSkewness.ShouldBe(scalarMoments.PopulationSkewness, tolerance: 1e-6);
        batchMoments.PopulationExcessKurtosis.ShouldBe(scalarMoments.PopulationExcessKurtosis, tolerance: 1e-6);
        batchCovariance.PopulationCovariance.ShouldBe(scalarCovariance.PopulationCovariance, tolerance: 1e-5);
        batchCovariance.Correlation.ShouldBe(scalarCovariance.Correlation, tolerance: 1e-8);
    }

    [Fact]
    public void SingleValueRangesMatchScalarUpdates()
    {
        var mean = new RunningMean();
        mean.AddRange([double.PositiveInfinity]);
        mean.Mean.ShouldBe(double.PositiveInfinity);

        var variance = new RunningMeanVariance();
        variance.AddRange([42.0]);
        variance.Mean.ShouldBe(42.0);
        variance.PopulationVariance.ShouldBe(0.0);

        var moments = new RunningMoments();
        moments.AddRange([42.0]);
        moments.Mean.ShouldBe(42.0);
        moments.PopulationVariance.ShouldBe(0.0);

        var covariance = new RunningCovariance();
        covariance.AddRange([2.0], [3.0]);
        covariance.MeanX.ShouldBe(2.0);
        covariance.MeanY.ShouldBe(3.0);
    }
}
