using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Tests.Optimization;

public class IslandPopulationTests
{
    [Fact]
    public void Constructor_SnapshotsIslands()
    {
        var island = Population.From([EvaluatedCandidate.From(1, new ObjectiveVector(1.0))]);
        var islands = new List<Population<int>> { island };
        var population = new IslandPopulation<int>(islands);

        islands.Clear();

        population.Islands.ShouldBe([island]);
    }
}
