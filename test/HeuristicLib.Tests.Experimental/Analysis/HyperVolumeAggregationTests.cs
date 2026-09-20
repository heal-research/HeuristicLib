namespace HEAL.HeuristicLib.Tests.Analysis;

public class HyperVolumeAggregationTests
{
    [Fact]
    public void ReusingSettings_CreatesIndependentParetoAccumulators()
    {
        var settings = new HyperVolumeAggregation<int>(new ObjectiveVector(5, 5));
        var first = ResolutionScope.Create().Resolve(settings);
        var second = ResolutionScope.Create().Resolve(settings);
        var objective = new ObjectiveDirections([ObjectiveDirection.Minimize, ObjectiveDirection.Minimize], NoTotalOrderComparer.Instance);

        var earlier = first.Aggregate([new EvaluatedCandidate<int>(1, new ObjectiveVector(1, 4))], objective);
        first.Aggregate([new EvaluatedCandidate<int>(2, new ObjectiveVector(4, 1))], objective).ShouldBe(7);
        second.Aggregate([new EvaluatedCandidate<int>(2, new ObjectiveVector(4, 1))], objective).ShouldBe(4);
        earlier.ShouldBe(4);
    }
}
