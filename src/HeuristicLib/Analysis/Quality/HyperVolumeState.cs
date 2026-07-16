using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Analysis.Quality;

public class HyperVolumeState<T>(ObjectiveVector referencePoint, ObjectiveDirections objective)
    : ParetoState<T>(referencePoint, objective)
{
    private Lazy<double>? hyperVolumeLazy;
    public double HyperVolume => hyperVolumeLazy?.Value ?? 0;

    public override bool AddPoints(IEnumerable<EvaluatedCandidate<T>> solutions)
    {
        if (!base.AddPoints(solutions))
            return false;

        hyperVolumeLazy = new Lazy<double>(() =>
            HyperVolumeCalculator.Calculate(
                Front.Select(x => x.ObjectiveVector),
                ReferencePoint,
                Objective));
        return true;
    }

    public override void Clear()
    {
        base.Clear();
        hyperVolumeLazy = null;
    }
}
