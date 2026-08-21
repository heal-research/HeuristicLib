namespace HEAL.HeuristicLib.Tests.Optimization;

public class PopulationTests
{
    [Fact]
    public void Constructor_SnapshotsEvaluatedCandidates()
    {
        var first = EvaluatedCandidate.From(1, new ObjectiveVector(1.0));
        var candidates = new List<EvaluatedCandidate<int>> { first };
        var population = new Population<int>(candidates);

        candidates.Clear();

        population.EvaluatedCandidates.ShouldBe([first]);
    }
}
