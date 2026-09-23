namespace HEAL.HeuristicLib.Tests.Analysis;

public class HyperVolumeAggregationTests
{
    /// <summary>
    /// The front an aggregation accumulates is its own, so two of them never see each other's points.
    /// </summary>
    [Fact]
    public void SeparateAggregations_AccumulateIndependentParetoFronts()
    {
        var first = new HyperVolumeAggregation<int>(new ObjectiveVector(5, 5));
        var second = new HyperVolumeAggregation<int>(new ObjectiveVector(5, 5));
        var objective = new ObjectiveDirections([ObjectiveDirection.Minimize, ObjectiveDirection.Minimize], NoTotalOrderComparer.Instance);

        var earlier = first.Aggregate([new EvaluatedCandidate<int>(1, new ObjectiveVector(1, 4))], objective);
        first.Aggregate([new EvaluatedCandidate<int>(2, new ObjectiveVector(4, 1))], objective).ShouldBe(7);
        second.Aggregate([new EvaluatedCandidate<int>(2, new ObjectiveVector(4, 1))], objective).ShouldBe(4);
        earlier.ShouldBe(4);
    }
}
