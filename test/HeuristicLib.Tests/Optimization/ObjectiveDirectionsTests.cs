using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Tests.Optimization;

public class ObjectiveDirectionsTests
{
    [Fact]
    public void Constructor_SnapshotsDirections()
    {
        var directions = new List<ObjectiveDirection> { ObjectiveDirection.Minimize };
        var objectives = new ObjectiveDirections(directions, NoTotalOrderComparer.Instance);

        directions[0] = ObjectiveDirection.Maximize;

        objectives.Directions.ShouldBe([ObjectiveDirection.Minimize]);
    }
}
