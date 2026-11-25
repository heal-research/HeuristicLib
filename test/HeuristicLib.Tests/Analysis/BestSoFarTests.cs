namespace HEAL.HeuristicLib.Tests.Analysis;

public class BestSoFarTests
{
    /// <summary>
    /// The first objective directions fix a best so far, because a best ranked by other directions is not comparable.
    /// </summary>
    [Fact]
    public void ACallWithOtherObjectiveDirections_Throws()
    {
        var best = new BestSoFar();
        best.Update([new ObjectiveVector(2.0)], SingleObjective.Minimize);

        Should.Throw<InvalidOperationException>(() => best.Update([new ObjectiveVector(1.0)], SingleObjective.Maximize));
    }

    /// <summary>
    /// Directions are compared by value, so equal directions of another problem continue the running best.
    /// </summary>
    [Fact]
    public void EqualDirectionsOfAnotherObject_ContinueTheRunningBest()
    {
        var best = new BestSoFar();
        var equalDirections = new ObjectiveDirections([ObjectiveDirection.Minimize], SingleObjective.Minimize.TotalOrderComparer);
        best.Update([new ObjectiveVector(2.0)], SingleObjective.Minimize);

        best.Update([new ObjectiveVector(3.0)], equalDirections).ShouldBe(new ObjectiveVector(2.0));
    }
}
