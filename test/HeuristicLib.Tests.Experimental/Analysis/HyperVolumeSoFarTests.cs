namespace HEAL.HeuristicLib.Tests.Analysis;

public class HyperVolumeSoFarTests
{
    /// <summary>
    /// The front a hypervolume-so-far object accumulates is its own, so two of them never see each other's points.
    /// </summary>
    [Fact]
    public void SeparateObjects_AccumulateIndependentParetoFronts()
    {
        var first = new HyperVolumeSoFar<int>(new ObjectiveVector(5, 5));
        var second = new HyperVolumeSoFar<int>(new ObjectiveVector(5, 5));
        var objective = new ObjectiveDirections([ObjectiveDirection.Minimize, ObjectiveDirection.Minimize], NoTotalOrderComparer.Instance);

        var earlier = first.Update([new EvaluatedCandidate<int>(1, new ObjectiveVector(1, 4))], objective);
        first.Update([new EvaluatedCandidate<int>(2, new ObjectiveVector(4, 1))], objective).ShouldBe(7);
        second.Update([new EvaluatedCandidate<int>(2, new ObjectiveVector(4, 1))], objective).ShouldBe(4);
        earlier.ShouldBe(4);
    }

    /// <summary>
    /// The first objective directions fix the front, because candidates ranked by other directions do not belong on it.
    /// </summary>
    [Fact]
    public void ACallWithOtherObjectiveDirections_Throws()
    {
        var hyperVolume = new HyperVolumeSoFar<int>(new ObjectiveVector(5, 5));
        hyperVolume.Update([new EvaluatedCandidate<int>(1, new ObjectiveVector(1, 4))], MultiObjective.Minimize(2));

        Should.Throw<InvalidOperationException>(() =>
            hyperVolume.Update([new EvaluatedCandidate<int>(2, new ObjectiveVector(4, 1))], MultiObjective.Create(false, true)));
    }
}
