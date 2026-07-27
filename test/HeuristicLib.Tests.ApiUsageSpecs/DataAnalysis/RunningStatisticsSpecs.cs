using HEAL.HeuristicLib.DataAnalysis.Statistics;
using Xunit;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.DataAnalysis;

public sealed class RunningStatisticsSpecs
{
    [Fact]
    public void RunningStatistics_AcceptScalarsBatchesAndIndependentPartitions()
    {
        var statistics = new RunningMeanVariance();
        statistics.Add(1.0);
        statistics.AddRange([2.0, 3.0]);

        var otherPartition = new RunningMeanVariance();
        otherPartition.AddRange([4.0, 5.0]);
        statistics.Merge(otherPartition);

        statistics.Count.ShouldBe(5);
        statistics.Mean.ShouldBe(3.0);
        statistics.PopulationVariance.ShouldBe(2.0);
    }
}
